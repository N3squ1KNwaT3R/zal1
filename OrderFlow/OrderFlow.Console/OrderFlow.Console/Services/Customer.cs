using System.Xml.Serialization;

namespace OrderFlow.Console.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string City { get; set; } = "";

    [XmlAttribute("isVip")]
    public bool IsVip { get; set; }
}
