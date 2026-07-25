// =============================================================================
// Copyright © 2024 DK (Freelancer)
// All rights reserved.
//
// Product:     DK GST Billing Platform
// Company:     DK (Freelancer)
// Website:     www.dkgstbilling.com
// Email:       support@dkgstbilling.com
//
// NOTICE: All information contained herein is, and remains the property of
// DK (Freelancer). The intellectual and technical
// concepts contained herein are proprietary to DK (Freelancer)
// and may be covered by Indian and International Patents,
// patents in process, and are protected by trade secret or copyright law.
//
// Unauthorized copying, modification, distribution, or use of this software,
// via any medium, is strictly prohibited without the prior written permission
// of DK (Freelancer).
// =============================================================================
using System.Globalization;
using GSTInvoice.Shared.DTOs.Invoice;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GSTInvoice.API.Services;

public class PdfService : IPdfService
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-IN");

    public byte[] GenerateInvoicePdf(InvoiceDto invoice, string sellerName, string sellerGstin, string sellerAddress)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Element(c => ComposeHeader(c, invoice, sellerName, sellerGstin, sellerAddress));
                page.Content().Element(c => ComposeContent(c, invoice));
                page.Footer().Element(c => ComposeFooter(c));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, InvoiceDto invoice, string sellerName, string sellerGstin, string sellerAddress)
    {
        container.Column(column =>
        {
            // Title
            column.Item().PaddingBottom(4).Text("TAX INVOICE")
                .FontSize(18).Bold().FontColor(Colors.Blue.Darken3);

            // Seller details
            column.Item().Text(text =>
            {
                text.Span("Seller: ").SemiBold();
                text.Span(sellerName);
            });
            column.Item().Text(text =>
            {
                text.Span("GSTIN: ").SemiBold();
                text.Span(sellerGstin);
            });
            column.Item().Text(text =>
            {
                text.Span("Address: ").SemiBold();
                text.Span(sellerAddress);
            });

            // Separator
            column.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Invoice details row
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Invoice #: ").SemiBold();
                    text.Span(invoice.InvoiceNumber);
                });
                row.RelativeItem().Text(text =>
                {
                    text.Span("Date: ").SemiBold();
                    text.Span(invoice.InvoiceDate.ToString("dd MMM yyyy"));
                });
                row.RelativeItem().Text(text =>
                {
                    text.Span("Due Date: ").SemiBold();
                    text.Span(invoice.DueDate.ToString("dd MMM yyyy"));
                });
            });

            // Client info
            if (!string.IsNullOrWhiteSpace(invoice.ClientName))
            {
                column.Item().PaddingTop(6).Text(text =>
                {
                    text.Span("Bill To: ").SemiBold();
                    text.Span(invoice.ClientName);
                });
            }

            column.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void ComposeContent(IContainer container, InvoiceDto invoice)
    {
        container.Column(column =>
        {
            // Items table
            column.Item().Table(table =>
            {
                // Column widths
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);   // #
                    columns.RelativeColumn(2);    // Description
                    columns.ConstantColumn(55);   // HSN
                    columns.ConstantColumn(50);   // Qty
                    columns.ConstantColumn(60);   // Rate
                    columns.ConstantColumn(45);   // GST%
                    columns.ConstantColumn(60);   // CGST
                    columns.ConstantColumn(60);   // SGST
                    columns.ConstantColumn(60);   // IGST
                    columns.ConstantColumn(65);   // Total
                });

                // Header row
                static void HeaderCell(IContainer cell, string text)
                {
                    cell.DefaultTextStyle(x => x.FontSize(9).SemiBold().FontColor(Colors.White))
                        .Background(Colors.Blue.Darken3)
                        .PaddingVertical(5).PaddingHorizontal(4)
                        .AlignCenter()
                        .Text(text);
                }

                table.Header(header =>
                {
                    header.Cell().Element(c => HeaderCell(c, "#"));
                    header.Cell().Element(c => HeaderCell(c, "Description"));
                    header.Cell().Element(c => HeaderCell(c, "HSN"));
                    header.Cell().Element(c => HeaderCell(c, "Qty"));
                    header.Cell().Element(c => HeaderCell(c, "Rate"));
                    header.Cell().Element(c => HeaderCell(c, "GST%"));
                    header.Cell().Element(c => HeaderCell(c, "CGST"));
                    header.Cell().Element(c => HeaderCell(c, "SGST"));
                    header.Cell().Element(c => HeaderCell(c, "IGST"));
                    header.Cell().Element(c => HeaderCell(c, "Total"));
                });

                // Data rows
                var index = 1;
                foreach (var item in invoice.Items)
                {
                    var rowIndex = index++;

                    static void DataCell(IContainer cell, string text, bool isNumber = false, bool alignCenter = false)
                    {
                        var container = cell
                            .Border(0.5f)
                            .BorderColor(Colors.Grey.Lighten2)
                            .PaddingVertical(4)
                            .PaddingHorizontal(4)
                            .DefaultTextStyle(x => x.FontSize(9));

                        if (alignCenter)
                            container.AlignCenter().Text(text);
                        else if (isNumber)
                            container.AlignRight().Text(text);
                        else
                            container.Text(text);
                    }

                    table.Cell().Element(c => DataCell(c, rowIndex.ToString(), alignCenter: true));
                    table.Cell().Element(c => DataCell(c, item.Description));
                    table.Cell().Element(c => DataCell(c, item.HSNCode));
                    table.Cell().Element(c => DataCell(c, item.Quantity.ToString("N2", Culture), true));
                    table.Cell().Element(c => DataCell(c, item.UnitPrice.ToString("N2", Culture), true));
                    table.Cell().Element(c => DataCell(c, $"{(int)item.GSTRate}%"));
                    table.Cell().Element(c => DataCell(c, item.CGSTAmount.ToString("N2", Culture), true));
                    table.Cell().Element(c => DataCell(c, item.SGSTAmount.ToString("N2", Culture), true));
                    table.Cell().Element(c => DataCell(c, item.IGSTAmount.ToString("N2", Culture), true));
                    table.Cell().Element(c => DataCell(c, item.TotalAmount.ToString("N2", Culture), true));
                }
            });

            // Summary section
            column.Item().PaddingTop(10).AlignRight().Width(300).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(summary =>
            {
                static void SummaryRow(ColumnDescriptor col, string label, string value, bool bold = false)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            if (bold)
                                text.Span(label).SemiBold();
                            else
                                text.Span(label);
                        });
                        row.ConstantItem(100).AlignRight().Text(text =>
                        {
                            if (bold)
                                text.Span(value).SemiBold();
                            else
                                text.Span(value);
                        });
                    });
                }

                SummaryRow(summary, "Subtotal", invoice.Subtotal.ToString("N2", Culture));
                SummaryRow(summary, "Discount", invoice.Discount.ToString("N2", Culture));
                SummaryRow(summary, "Taxable", invoice.TaxableAmount.ToString("N2", Culture));

                summary.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                SummaryRow(summary, "CGST", invoice.TotalCGST.ToString("N2", Culture));
                SummaryRow(summary, "SGST", invoice.TotalSGST.ToString("N2", Culture));
                SummaryRow(summary, "IGST", invoice.TotalIGST.ToString("N2", Culture));

                summary.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                SummaryRow(summary, "Grand Total", $"₹ {invoice.GrandTotal.ToString("N2", Culture)}", bold: true);
            });

            // Notes & Terms
            if (!string.IsNullOrWhiteSpace(invoice.Notes))
            {
                column.Item().PaddingTop(10).Text(text =>
                {
                    text.Span("Notes: ").SemiBold();
                    text.Span(invoice.Notes);
                });
            }

            if (!string.IsNullOrWhiteSpace(invoice.Terms))
            {
                column.Item().PaddingTop(4).Text(text =>
                {
                    text.Span("Terms: ").SemiBold();
                    text.Span(invoice.Terms);
                });
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Generated by DK GST Billing Platform").FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }
}

