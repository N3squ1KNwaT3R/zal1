using System.Xml.Linq;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Persistence;

public class XmlReportBuilder
{
    public XDocument BuildReport(IEnumerable<Order> orders)
    {
        var list = orders.ToList();

        var byStatus = list
            .GroupBy(o => o.Status)
            .OrderByDescending(g => g.Sum(o => o.TotalAmount))
            .Select(g => new XElement("status",
                new XAttribute("name",    g.Key.ToString()),
                new XAttribute("count",   g.Count()),
                new XAttribute("revenue", g.Sum(o => o.TotalAmount))));

        var byCustomer = list
            .GroupBy(o => o.Customer, c => c, (c, os) => (Customer: c, Orders: os.ToList()))
            .OrderByDescending(x => x.Orders.Sum(o => o.TotalAmount))
            .Select(x => new XElement("customer",
                new XAttribute("id",    x.Customer.Id),
                new XAttribute("name",  x.Customer.Name),
                new XAttribute("isVip", x.Customer.IsVip.ToString().ToLower()),
                new XElement("orderCount", x.Orders.Count),
                new XElement("totalSpent", x.Orders.Sum(o => o.TotalAmount)),
                new XElement("orders",
                    x.Orders.Select(o => new XElement("orderRef",
                        new XAttribute("id",    o.Id),
                        new XAttribute("total", o.TotalAmount))))));

        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("report",
                new XAttribute("generated", DateTime.Now.ToString("o")),
                new XElement("summary",
                    new XAttribute("totalOrders",  list.Count),
                    new XAttribute("totalRevenue", list.Sum(o => o.TotalAmount))),
                new XElement("byStatus",  byStatus),
                new XElement("byCustomer", byCustomer)));
    }

    public async Task SaveReportAsync(XDocument report, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        await report.SaveAsync(stream, SaveOptions.None, CancellationToken.None);
    }

    public async Task<IEnumerable<int>> FindHighValueOrderIdsAsync(string reportPath, decimal threshold)
    {
        await using var stream = new FileStream(reportPath, FileMode.Open, FileAccess.Read);
        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

        return doc.Descendants("orderRef")
            .Where(el => decimal.Parse(
                el.Attribute("total")!.Value,
                System.Globalization.CultureInfo.InvariantCulture) > threshold)
            .Select(el => (int)el.Attribute("id")!)
            .Distinct()
            .OrderBy(id => id);
    }
}
