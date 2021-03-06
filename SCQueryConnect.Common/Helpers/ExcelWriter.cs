﻿using Microsoft.Office.Interop.Excel;
using SCQueryConnect.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;

namespace SCQueryConnect.Common.Helpers
{
    public class ExcelWriter : IExcelWriter
    {
        private readonly HashSet<string> _validFileExtensions
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".xls",
            ".xlsb",
            ".xlsm",
            ".xlsx"
        };

        public string GetValidFilename(string filename)
        {
            var ext = Path.GetExtension(filename);
            var isValid = _validFileExtensions.Contains(ext);

            var suffix = isValid
                ? string.Empty
                : ".xlsx";

            var toReturn = $"{filename}{suffix}";
            return toReturn;
        }

        public void RewriteExcelFile(string path)
        {
            var excel = (_Application)new Application
            {
                ScreenUpdating = false,
                UserControl = false,
                Visible = false
            };

            try
            {
                var workbook = (_Workbook)excel.Workbooks.Open(path);
                excel.ScreenUpdating = false;

                workbook.SaveAs(path);
                workbook.Close();
            }
            finally
            {
                excel.Quit();
            }
        }

        public void WriteToExcel(
            string filename,
            string[,] itemsData,
            string[,] relationshipsData)
        {
            //Start Excel and get Application object.
            var excel = (_Application) new Application
            {
                ScreenUpdating = false,
                UserControl = false,
                Visible = false
            };

            excel.SheetsInNewWorkbook = 2;

            //Get a new workbook.
            var workbook = (_Workbook) excel.Workbooks.Add(Missing.Value);
            excel.ScreenUpdating = false;

            WriteData(itemsData, (_Worksheet) workbook.Sheets[1], "Items");
            WriteData(relationshipsData, (_Worksheet) workbook.Sheets[2], "Relationships");

            workbook.SaveAs(filename);
            workbook.Close();
        }

        private void WriteData(
            string[,] data,
            _Worksheet worksheet,
            string sheetName)
        {
            worksheet.Name = sheetName;

            var columnCount = data.GetLength(0);
            var rowCount = data.GetLength(1);

            for (var c = 0; c < columnCount; c++)
            {
                for (var r = 0; r < rowCount; r++)
                {
                    var dataItem = WebUtility.HtmlDecode(data[c, r]);
                    worksheet.Cells[c + 1, r + 1] = dataItem;
                }
            }
        }
    }
}
