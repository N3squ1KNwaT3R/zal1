using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace OrderFlow.Console.Models;

public class OrderItem
{
    public Product Product { get; set; } = null!;

    [XmlElement("quantity")]
    public int Quantity { get; set; }

    [JsonIgnore]
    [XmlIgnore]
    public decimal TotalPrice => Product.Price * Quantity;
}
