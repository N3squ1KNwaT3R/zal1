using System.Xml.Serialization;

namespace OrderFlow.Console.Models;

public class Product
{
    public int Id { get; set; }

    [XmlElement("productName")]
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}
