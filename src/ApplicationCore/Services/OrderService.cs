using System.Linq;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using System.Net.Http;
using System;
using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.GetBySpecAsync(basketSpec);

        Guard.Against.NullBasket(basketId, basket);
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);

        //Call AzureFunction
        using (var clientH = new HttpClient())
        {
            clientH.BaseAddress = new Uri("https://eshopfunc.azurewebsites.net");
            //client.BaseAddress = new Uri("http://localhost:7071");
            var content = new System.Net.Http.StringContent(JsonSerializer.Serialize(order), System.Text.Encoding.UTF8, "application/json");
            var result = await clientH.PostAsync("/api/OrderToCosmos?", content);
        }

        //__ Add order to azure service bus
        const string ServiceBusConnectionString = "Endpoint=sb://eshopsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=zcC7mafLEU8cKqYG+gkDuR27hWeS5nUgH2kD9i+gldE=";
        const string QueueName = "orders";

        await using var client = new ServiceBusClient(ServiceBusConnectionString);

        await using ServiceBusSender sender = client.CreateSender(QueueName);
        try
        {
            var content = JsonSerializer.Serialize(order);
            var message = new ServiceBusMessage(content);
            await sender.SendMessageAsync(message);
        }
        finally
        {
            await sender.DisposeAsync();
            await client.DisposeAsync();
        }

        await _orderRepository.AddAsync(order);
    }
}
