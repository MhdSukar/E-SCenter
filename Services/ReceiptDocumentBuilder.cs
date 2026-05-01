using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using ESCenter.Models;

namespace ESCenter.Services
{
    public static class ReceiptDocumentBuilder
    {
        public static FlowDocument Build(RepairTicket ticket)
        {
            var flowDoc = new FlowDocument
            {
                PageWidth = 400,
                PagePadding = new Thickness(30),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };

            flowDoc.Blocks.Add(new Paragraph(new Run("E-SCenter Repair Receipt"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            });

            flowDoc.Blocks.Add(CreateDividerParagraph(8));

            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(120) });
            table.Columns.Add(new TableColumn { Width = new GridLength(280) });

            var group = new TableRowGroup();
            AddRow(group, "ESC-ID:", ticket.EscTicketId);
            AddRow(group, "Date:", ticket.ReceiveDate.ToString("yyyy-MM-dd HH:mm"));
            AddRow(group, "Customer:", ticket.CustomerName);
            AddRow(group, "Phone:", ticket.PhoneNumber);
            AddRow(group, "Device:", $"{ticket.DeviceBrand} {ticket.DeviceModel}".Trim());
            AddRow(group, "Category:", ticket.DeviceCategory);
            AddRow(group, "Serial/IMEI:", string.IsNullOrWhiteSpace(ticket.SerialIMEI) ? "N/A" : ticket.SerialIMEI);
            AddRow(group, "Problem:", string.IsNullOrWhiteSpace(ticket.ProblemDescription) ? "N/A" : ticket.ProblemDescription);
            AddRow(group, "Status:", ticket.RepairStatus);
            AddRow(group, "Priority:", ticket.PriorityLevel);

            if (ticket.EstimatedCost.HasValue)
            {
                AddRow(group, "Estimated:", $"{ticket.EstimatedCost:N0} {ticket.EstimatedCostCurrency}");
            }

            if (ticket.FinalCost.HasValue)
            {
                AddRow(group, "Final Cost:", $"{ticket.FinalCost:N0} {ticket.FinalCostCurrency}");
            }

            if (ticket.HasWarranty)
            {
                AddRow(group, "Warranty:", ticket.WarrantyPeriod);
            }

            if (!string.IsNullOrWhiteSpace(ticket.Notes))
            {
                AddRow(group, "Notes:", ticket.Notes);
            }

            table.RowGroups.Add(group);
            flowDoc.Blocks.Add(table);

            flowDoc.Blocks.Add(CreateDividerParagraph(8));

            flowDoc.Blocks.Add(new Paragraph(new Run("Thank you for choosing E-SCenter"))
            {
                FontSize = 11,
                TextAlignment = TextAlignment.Center,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0)
            });

            if (ticket.IsReadyForPickup)
            {
                flowDoc.Blocks.Add(new Paragraph(new Run("★ READY FOR PICKUP ★"))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(Colors.DarkGreen),
                    Margin = new Thickness(0, 8, 0, 0)
                });
            }

            return flowDoc;
        }

        public static string BuildPlainText(RepairTicket ticket)
        {
            var text = new StringBuilder();
            text.AppendLine("E-SCenter Repair Receipt");
            text.AppendLine("================================");
            text.AppendLine($"ESC-ID  : {ticket.EscTicketId}");
            text.AppendLine($"Date    : {ticket.ReceiveDate:yyyy-MM-dd HH:mm}");
            text.AppendLine($"Customer: {ticket.CustomerName}");
            text.AppendLine($"Phone   : {ticket.PhoneNumber}");
            text.AppendLine($"Device  : {$"{ticket.DeviceBrand} {ticket.DeviceModel}".Trim()}");
            text.AppendLine($"Category: {ticket.DeviceCategory}");
            text.AppendLine($"Serial/IMEI: {(string.IsNullOrWhiteSpace(ticket.SerialIMEI) ? "N/A" : ticket.SerialIMEI)}");
            text.AppendLine($"Problem : {(string.IsNullOrWhiteSpace(ticket.ProblemDescription) ? "N/A" : ticket.ProblemDescription)}");
            text.AppendLine($"Status  : {ticket.RepairStatus}");
            text.AppendLine($"Priority: {ticket.PriorityLevel}");

            if (ticket.EstimatedCost.HasValue)
            {
                text.AppendLine($"Estimated: {ticket.EstimatedCost:N0} {ticket.EstimatedCostCurrency}");
            }

            if (ticket.FinalCost.HasValue)
            {
                text.AppendLine($"Final Cost: {ticket.FinalCost:N0} {ticket.FinalCostCurrency}");
            }

            if (ticket.HasWarranty)
            {
                text.AppendLine($"Warranty: {ticket.WarrantyPeriod}");
            }

            if (!string.IsNullOrWhiteSpace(ticket.Notes))
            {
                text.AppendLine($"Notes   : {ticket.Notes}");
            }

            if (ticket.IsReadyForPickup)
            {
                text.AppendLine();
                text.AppendLine("★ READY FOR PICKUP ★");
            }

            return text.ToString();
        }

        private static Paragraph CreateDividerParagraph(double bottomMargin)
        {
            return new Paragraph(new Run("────────────────────────────────────────"))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, bottomMargin)
            };
        }

        private static void AddRow(TableRowGroup group, string label, string? value)
        {
            var row = new TableRow();

            row.Cells.Add(new TableCell(new Paragraph(new Run(label))
            {
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.DimGray),
                Margin = new Thickness(0)
            })
            { Padding = new Thickness(0, 2, 6, 2) });

            row.Cells.Add(new TableCell(new Paragraph(new Run(string.IsNullOrWhiteSpace(value) ? "N/A" : value))
            {
                Foreground = new SolidColorBrush(Colors.Black),
                Margin = new Thickness(0)
            })
            { Padding = new Thickness(0, 2, 0, 2) });

            group.Rows.Add(row);
        }
    }
}
