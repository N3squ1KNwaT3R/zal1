using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace OrderFlow.Console.Models;

public class Order
{
    [JsonPropertyName("orderId")]
    [XmlAttribute("id")]
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public List<OrderItem> Items { get; set; } = new();

    public OrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Notes { get; set; }

    [JsonIgnore]
    [XmlIgnore]
    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
}
