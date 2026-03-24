namespace OrderFlow.Console.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string City { get; set; } = "";
    public bool IsVip { get; set; }
}