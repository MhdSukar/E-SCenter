using System;
using System.Windows.Controls;
using System.Windows.Documents;
using ESCenter.Models;
using ESCenter.Core;

namespace ESCenter.Services
{
    public class ReceiptPrintService
    {
        public void PrintReceipt(RepairTicket ticket)
        {
            if (ticket == null)
            {
                AppLogger.Warning("Print receipt requested with no ticket.");
                return;
            }

            try
            {
                FlowDocument flowDoc = ReceiptDocumentBuilder.Build(ticket);
                var dialog = new PrintDialog();
                if (dialog.ShowDialog() == true)
                {
                    dialog.PrintDocument(((IDocumentPaginatorSource)flowDoc).DocumentPaginator, "ESC Repair Receipt");
                    AppLogger.Success("Receipt print job sent.");
                }
                else
                {
                    AppLogger.Info("Receipt print canceled by user.");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to print receipt: {ex.Message}");
            }
        }
    }
}
