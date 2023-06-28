using System;
using System.Linq;
using System.Text;
using MobileWarehouseOnline.Frame.Interfaces.QuickActions;
using MobileWarehouseOnline.Frame.Plugins;
using MobileWarehouseOnline.Frame.Types.Enums;
using MobileWarehouseOnline.Processes.Interfaces.Presentation;
using System.Windows.Forms;
using MobileWarehouseOnline.Presentation.Interfaces.Controls.Editors;
using System.Net.Sockets;
using System.IO;

namespace AutoScanPlugin
{
    [Plugin(typeof(IProcessForm), "AutoScanPlugin", Description = "AutoScanPlugin")]
    public class AutoScan : PluginBase
    {
        private IProcessForm ProcForm { get; set; }

        BaseQuickAction ScanQuickAction;
        public override void OnInitialize()
        {
            base.OnInitialize();

            ProcForm = (IProcessForm)this.Form;

            ScanQuickAction = new BaseQuickAction("ScanQuickActionId", "AUTO SKAN", KeyboardKey.None, () => OnScanAction());
            ProcForm.QuickActions.Add(ScanQuickAction);
        }

        private void OnScanAction()
        {
            if (ProcForm.ActionBar.MainButton.Text.Contains("Kod jednostki logistycznej"))
            {
                foreach (var control in ((Control)ProcForm.MainContainer).Controls[0].Controls.OfType<IMobTextBoxButton>())
                {
                    string code = GetCode();

                    if (!code.Contains("Error"))
                    {
                        Api.Toast.ShowSuccess("Komunikacja zakończona - OK!");
                        control.Text = code;
                    }
                    else
                    {
                        Api.Toast.ShowError($@"Komunikacja zakończona - {code}!");
                    }

                }
            }
            else
            {
                Api.MessagePanel.Show("Plugin warning! (-1)\n\nAUTO SKAN dostępny tylko do skanowania jednostek logistycznych.");
            }
        }

        private string GetCode()
        {
            try
            {

                var ip = Api.SqlManager.ExecuteScalar<string>($@"IF (EXISTS (SELECT *  FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'cdn' AND  TABLE_NAME = 'WMSPluginSettings'))
								BEGIN
								    if(EXISTS (select Value from cdn.WMSPluginSettings where Name='IP'))
									BEGIN
										select Value from cdn.WMSPluginSettings where Name='IP'
									END
									ELSE
									BEGIN
										select 'Error (-4)'
									END
								END
								ELSE
								BEGIN
									select 'Error (-3)'
								END", System.Data.CommandType.Text);

 		    if(!ip.Contains("Error"))
			return ip;

		    TcpClient tcpclnt = new TcpClient();
		    tcpclnt.Connect(ip, 2112);
		    Stream stm = tcpclnt.GetStream();
	
		    ASCIIEncoding asen = new ASCIIEncoding();
		    byte[] message = new byte[] { 0x02, 0x32, 0x31, 0x03 };
	
		    stm.Write(message, 0, message.Length);
	
		    byte[] bb = new byte[100];
		    int k = stm.Read(bb, 0, 100);
	
		    string reading = "";
	
		    for (int i = 0; i < k; i++)
		    {
			reading = reading + Convert.ToChar(bb[i]).ToString();
		    }
	
		    tcpclnt.Close();
	
		    if(reading != "")
		    {
			return Cut(reading);
		    }
		    else
		    {
			return "Error (-1)";
		    }
      
            }
            catch
            {
                return "Error (-2)";
            }
        }
    }
}
