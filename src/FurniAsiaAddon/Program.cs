using FurniAsiaAddon.Models;
using FurniAsiaAddon.Services;
using SAPbouiCOM.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FurniAsiaAddon
{
    class Program
    {
        static SAPbobsCOM.Company oCom;
        static SAPbobsCOM.Recordset oRS;

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Application oApp = null;
                if (args.Length < 1)
                {
                    oApp = new Application();
                }
                else
                {
                    oApp = new Application(args[0]);
                }

                Application.SBO_Application.AppEvent += new SAPbouiCOM._IApplicationEvents_AppEventEventHandler(SBO_Application_AppEvent);
                Application.SBO_Application.ItemEvent += SBO_Application_ItemEvent;


                oCom = (SAPbobsCOM.Company)Application.SBO_Application.Company.GetDICompany();
                oRS = (SAPbobsCOM.Recordset)oCom.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                oApp.Run();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }

        private static void SBO_Application_ItemEvent(string FormUID, ref SAPbouiCOM.ItemEvent pVal, out bool BubbleEvent)
        {
            BubbleEvent = true;
            if (pVal.FormTypeEx == "157" && pVal.BeforeAction == false && pVal.EventType == SAPbouiCOM.BoEventTypes.et_FORM_LOAD)
            {
                SAPbouiCOM.Form oForm = Application.SBO_Application.Forms.Item(FormUID);
                SAPbouiCOM.Item oButtonPurchase = oForm.Items.Add("Script", SAPbouiCOM.BoFormItemTypes.it_BUTTON);
                SAPbouiCOM.Item oTempItem = oForm.Items.Item("2");
                SAPbouiCOM.Button oPostButton = (SAPbouiCOM.Button)oButtonPurchase.Specific;

                oPostButton.Caption = "Докон база енгилаш";
                oButtonPurchase.Left = oTempItem.Left + oTempItem.Width + 5;
                oButtonPurchase.Top = oTempItem.Top;
                oButtonPurchase.Width = 130;
                oButtonPurchase.Height = oTempItem.Height;
                oButtonPurchase.AffectsFormMode = false;
            }

            if (pVal.FormTypeEx == "157" && pVal.BeforeAction == false && pVal.EventType == SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED && pVal.ItemUID == "Script")
            {
                HandleItemEventAsync(FormUID).Wait();
            }
        }

        private static async Task HandleItemEventAsync(string FormUID)
        {
            try
            {
                SAPbouiCOM.Form oForm = Application.SBO_Application.Forms.Item(FormUID);
                SAPbouiCOM.Matrix oMatrix = (SAPbouiCOM.Matrix)oForm.Items.Item("3").Specific;
                List<Item> items = new List<Item>();

                Application.SBO_Application.StatusBar.SetSystemMessage("Loading ... ", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
                oRS.DoQuery($"SELECT T0.\"ItemCode\", T0.\"PriceList\", T0.\"Price\", T0.\"Currency\", T0.\"U_shtuk_price\" FROM \"FURNI_PROD_2023\".\"ITM1\" T0 INNER JOIN \"SHOP_2023\".\"ITM1\" T1 ON T0.\"ItemCode\" = T1.\"ItemCode\" WHERE T0.\"PriceList\" = 1 AND T1.\"PriceList\" = 1 AND T0.\"U_Status\" = 'YES' ORDER BY T0.\"ItemCode\";");

                if (oRS.RecordCount == 0)
                {
                    Application.SBO_Application.StatusBar.SetSystemMessage("No items entered. Please add items and try again.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                    return;
                }

                for (int i = 1; i <= oRS.RecordCount; i++)
                {
                    string price = oRS.Fields.Item("Price").Value.ToString();
                    string u_shtuk_price = oRS.Fields.Item("U_shtuk_price").Value.ToString();

                    if ((!string.IsNullOrEmpty(price) && price != "0") && (!string.IsNullOrEmpty(u_shtuk_price) && u_shtuk_price != "0"))
                    {
                        Item item = new Item()
                        {
                            ItemCode = oRS.Fields.Item("ItemCode").Value.ToString(),
                            Currency = oRS.Fields.Item("Currency").Value.ToString(),
                            PackagePrice = decimal.Parse(u_shtuk_price),
                            Price = decimal.Parse(price)
                        };
                        items.Add(item);
                    }
                    oRS.MoveNext();
                }

                ILoginService loginService = new LoginService();
                var token = loginService.SendLoginRequest();

                SAPbouiCOM.ProgressBar oProgressBar = Application.SBO_Application.StatusBar.CreateProgressBar("Progress", items.Count, false);
                oProgressBar.Value = 0;

                foreach (var item in items)
                {
                    Console.WriteLine(item.ItemCode + " " + item.Currency + " " + item.Price + " " + item.PackagePrice + " " + item.PriceList);

                    var result = await loginService.SendPatchRequestAsync(token, item);
                    Console.WriteLine(result);
                    oProgressBar.Value += 1;
                }

                oProgressBar.Stop();

                string query = "UPDATE ITM1 T0 SET T0.\"U_Status\" = 'NO' WHERE T0.\"ItemCode\" IN (";

                foreach (var item in items)
                {
                    query += $"'{item.ItemCode}',";
                }

                query = query.Trim(',') + ")";
                oRS.DoQuery(query);

                Application.SBO_Application.StatusBar.SetSystemMessage("Success ... ", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
            }
            catch (Exception exception)
            {
                Application.SBO_Application.StatusBar.SetSystemMessage("Error occured ... ", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Error);
                Console.WriteLine(exception.Message);
                Console.WriteLine(exception.Data);
            }
        }

        static void SBO_Application_AppEvent(SAPbouiCOM.BoAppEventTypes EventType)
        {
            switch (EventType)
            {
                case SAPbouiCOM.BoAppEventTypes.aet_ShutDown:
                    //Exit Add-On
                    System.Windows.Forms.Application.Exit();
                    break;
                case SAPbouiCOM.BoAppEventTypes.aet_CompanyChanged:
                    break;
                case SAPbouiCOM.BoAppEventTypes.aet_FontChanged:
                    break;
                case SAPbouiCOM.BoAppEventTypes.aet_LanguageChanged:
                    break;
                case SAPbouiCOM.BoAppEventTypes.aet_ServerTerminition:
                    break;
                default:
                    break;
            }
        }
    }
}
