using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace OrderFlow.Console.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [XmlElement("quantity")]
    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    [JsonIgnore]
    [XmlIgnore]
    public decimal TotalPrice => UnitPrice * Quantity;
}
