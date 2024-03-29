﻿using FOAEA3.Model;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using Spire.Pdf.Widget;

namespace FOAEA3.Common.Helpers
{
    public class PdfHelper
    {
        public static string LastError { get; set; }

        public static List<string> FillPdf(string templatePath, string outputPath, Dictionary<string, string> values, bool isEnglish = true)
        {
            LastError = string.Empty;
            var pdfDoc = CreatePdfFromTemplate(templatePath, values, out List<string> missingFields, out List<string> foundFields, isEnglish);

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            pdfDoc.SaveToFile(outputPath);
            pdfDoc.Close();

            return missingFields;
        }

        public static (MemoryStream, List<string>) FillPdf(string templatePath, Dictionary<string, string> values, bool isEnglish = true)
        {
            LastError = string.Empty;
            var pdfDoc = CreatePdfFromTemplate(templatePath, values, out List<string> missingFields, out List<string> foundFields, isEnglish);

            var data = pdfDoc.SaveToStream(FileFormat.PDF);

            if (data is not null)
                return (data[0] as MemoryStream, missingFields);
            else
                return (null, null);
        }

        public static Dictionary<string, string> GetValuesForPDF(short year, List<TraceFinancialResponseDetailValueData> finValues, List<CraFieldData> craFields)
        {
            var values = new Dictionary<string, string>();

            foreach (var value in finValues)
            {
                string fieldName = value.FieldName;
                string fieldValue = value.FieldValue;

                var thisCraField = craFields.Where(m => m.CRAFieldName == fieldName).FirstOrDefault();
                if (thisCraField is not null)
                {
                    string pdfFieldName;

                    if (year >= 2019)
                        pdfFieldName = thisCraField.CRAFieldCode;
                    else
                        pdfFieldName = thisCraField.CRAFieldCodeOld;

                    if (pdfFieldName == "MaritalStatus")
                    {
                        switch (fieldValue)
                        {
                            case "01": pdfFieldName = "Married"; break;
                            case "02": pdfFieldName = "CommonLaw"; break;
                            case "03": pdfFieldName = "Widowed"; break;
                            case "04": pdfFieldName = "Divorced"; break;
                            case "05": pdfFieldName = "Separated"; break;
                            case "06": pdfFieldName = "Single"; break;
                        }
                        fieldValue = "1";
                    }
                    if (pdfFieldName == "PreferredLanguage")
                    {
                        switch (fieldValue)
                        {
                            case "E": pdfFieldName = "English"; break;
                            case "F": pdfFieldName = "French"; break;
                        }
                        fieldValue = "1";
                    }

                    if (!string.IsNullOrEmpty(pdfFieldName))
                    {
                        pdfFieldName = pdfFieldName.ToUpper();

                        if (!values.ContainsKey(pdfFieldName))
                            values.Add(pdfFieldName, fieldValue);
                    }
                }
            }

            return values;
        }

        private static PdfDocument CreatePdfFromTemplate(string templatePath, Dictionary<string, string> values,
                                                         out List<string> missingFields, out List<string> foundFields,
                                                         bool isEnglish = true)
        {
            var pdfDoc = new PdfDocument();
            try
            {
                pdfDoc.LoadFromFile(templatePath);
            }
            catch
            {
                LastError = $"Failed to load {templatePath}";
            }

            missingFields = new List<string>();
            foundFields = new List<string>();
            var standardFont = new PdfFont(PdfFontFamily.TimesRoman, 12f, PdfFontStyle.Regular);

            var form = pdfDoc.Form as PdfFormWidget;

            if (form is not null)
                for (int i = 0; i < form.FieldsWidget.List.Count; i++)
                {
                    var field = form.FieldsWidget.List[i] as Spire.Pdf.Fields.PdfField;

                    if (field is not null)
                    {
                        string fieldType = field.GetType().Name.ToUpper();
                        switch (fieldType)
                        {
                            case "PDFTEXTBOXFIELDWIDGET":
                                var textBox = field as PdfTextBoxFieldWidget;
                                if (textBox is not null)
                                {
                                    var fieldName = textBox.Name.ToUpper();
                                    if (values.ContainsKey(fieldName))
                                    {
                                        textBox.Font = standardFont;
                                        textBox.Text = values[fieldName];
                                        foundFields.Add(fieldName);
                                    }
                                }
                                break;
                            case "PDFCHECKBOXWIDGETFIELDWIDGET":
                                var checkBox = field as PdfCheckBoxWidgetFieldWidget;
                                if (checkBox is not null)
                                {
                                    var fieldName = checkBox.Name.ToUpper();
                                    if (values.ContainsKey(fieldName))
                                    {
                                        checkBox.Checked = values[fieldName] == "1";
                                        foundFields.Add(fieldName);
                                    }
                                }
                                break;
                            case "PDFCOMBOBOXWIDGETFIELDWIDGET":
                                var comboBox = field as PdfComboBoxWidgetFieldWidget;
                                if (comboBox is not null)
                                {
                                    var fieldName = comboBox.Name.ToUpper();
                                    if (values.ContainsKey(fieldName))
                                    {
                                        // var comboValues = comboBox.Values;
                                        comboBox.SelectedValue = values[fieldName];
                                    }
                                }
                                break;
                            case "PDFRADIOBUTTONLISTFIELDWIDGET":
                                var radioButton = field as PdfRadioButtonListFieldWidget;
                                if (radioButton is not null)
                                {
                                    var fieldName = radioButton.Name.ToUpper();
                                    if (values.ContainsKey(fieldName))
                                    {
                                        // var comboValues = comboBox.Values;
                                        radioButton.SelectedValue = values[fieldName];
                                    }
                                }
                                break;
                            default:
                                // TODO: Log invalid types?
                                break;
                        }
                    }
                }

            foreach (var value in values)
                if (!foundFields.Contains(value.Key))
                    missingFields.Add(value.Key);

            WatermarkPDF(ref pdfDoc, isEnglish);

            pdfDoc.DocumentInformation.Title = "GeneratedPDF";
            pdfDoc.Form.IsFlatten = true;

            return pdfDoc;
        }

        private static void WatermarkPDF(ref PdfDocument pdf, bool isEnglish = true)
        {
            var fontSmall = new PdfFont(PdfFontFamily.Helvetica, 9f, PdfFontStyle.Bold);
            var fontSmallItalic = new PdfFont(PdfFontFamily.Helvetica, 9f, PdfFontStyle.Italic | PdfFontStyle.Bold);
            var fontBig = new PdfFont(PdfFontFamily.Helvetica, 14f, PdfFontStyle.Bold);

            var brush = PdfBrushes.CornflowerBlue;

            string textLine1;
            string textLine2;
            string textLine3 = string.Empty;
            string textLine2Italic;
            string textREPLICA = "REPLICA";

            if (isEnglish)
            {
                textLine1 = "Important Notice: Populated by the Department of Justice Canada based on taxpayer information";                    
                textLine2 = "shared by the Canada Revenue Agency for the purposes of releasing information under Part I of the";
                textLine2Italic = "Family Orders and Agreements Enforcement Assistance Act";
            }
            else
            {
                textLine1 = "Avis important : Rempli par le ministère de la Justice du Canada à partir de renseignements des";
                textLine2 = "contribuables partagés par l'Agence du revenu du Canada aux fins de la communication de renseignements";
                textLine3 = "en vertu de la partie I de la";
                textLine2Italic = "Loi d'aide à l'exécution des ordonnances et des ententes familiales";
            }

            try
            {
                pdf.Pages[0].DeleteImage(0);
            }
            catch
            {
                // ignore if no image was found
            }

            foreach (PdfPageBase page in pdf.Pages)
            {
                page.Canvas.Save();

                page.Canvas.DrawString(textLine1, fontSmall, brush, 24, -1);
                page.Canvas.DrawString(textLine2, fontSmall, brush, 24, 9);
                if (!isEnglish)
                    page.Canvas.DrawString(textLine3, fontSmall, brush, 24, 19);
                page.Canvas.DrawString(textLine2Italic, fontSmallItalic, brush, isEnglish ? 24 : 140, 19);
                page.Canvas.DrawString(textREPLICA, fontBig, brush, 520, 0);

                page.Canvas.Restore();
            }
        }
    }
}
