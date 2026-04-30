using System.Xml.Serialization;

namespace OrderFlow.Console.Models;

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string City { get; set; } = "";
    public string? Email { get; set; }

    [XmlAttribute("isVip")]
    public bool IsVip { get; set; }
}
