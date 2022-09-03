using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;

namespace Serial_Tester
{
    public partial class Form1 : Form
    {
        private SerialComm serialComm;
        
        public Form1()
        {
            InitializeComponent();
            serialComm = new SerialComm();
            FillPortBox(portComboBox);
            portComboBox.SelectedIndex = 0;

            serialComm.OnPortClosed += SerialComm_OnPortClosed;
            serialComm.OnPortOpened += SerialComm_OnPortOpened;
            serialComm.OnPortFailed += SerialComm_OnPortFailed;
            serialComm.OnNewLineArrived += SerialComm_OnNewLineArrived;
        }

        private void FillPortBox(ComboBox portBox)
        {
            foreach (string port in SerialComm.GetPortNames())
            {
                portBox.Items.Add(port);
            }
        }
        private void SafeControlAccess(Control control, string text, bool enabled = true)
        {
            if (control == null)
                throw new ArgumentNullException();

            if (control.InvokeRequired)
            {
                control.Invoke(new MethodInvoker(delegate
                {
                    if (control is TextBox)
                        (control as TextBox).AppendText(text);
                    else
                        control.Text = text;
                    control.Enabled = enabled;
                }
                ));
            }
            else
            {
                control.Text = text;
                control.Enabled = enabled;
            }

        }

        private void SerialComm_OnNewLineArrived(object sender, EventArgs e)
        {
            SafeControlAccess(dataTextBox, serialComm.GetIncomingData + "\r\n", true);
            serialComm.AsyncReadLine();
        }

        private void SerialComm_OnPortFailed(object sender, EventArgs e)
        {
            sendTimer.Enabled = false;
            SafeControlAccess(connectButton, "FAIL", true);
            SafeControlAccess(startButton, "Iniciar", false);
        }

        private void SerialComm_OnPortOpened(object sender, EventArgs e)
        {
            SafeControlAccess(connectButton, "Disconnect");
            SafeControlAccess(startButton, "Iniciar", true);
            serialComm.AsyncReadLine();
        }

        private void SerialComm_OnPortClosed(object sender, EventArgs e)
        {
            SafeControlAccess(connectButton, "Connect");
            SafeControlAccess(startButton, "Iniciar", true);
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            if (serialComm.IsOpen)
                serialComm.Close();
            else
            {
                serialComm.Config(9600, portComboBox.SelectedItem.ToString());
                serialComm.Open();
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            if (sendTimer.Enabled == true)
            {
                sendTimer.Enabled = false;
                startButton.Text = "Iniciar";
            }
            else
            {
                sendTimer.Enabled = true;
                startButton.Text = "Detener";
            }
        }

        private void sendTimer_Tick(object sender, EventArgs e)
        {
            string[] dataToSend = RandomData.GetRandomData();

            serialComm.AsyncWriteLine(dataToSend[0]);
            serialComm.AsyncWriteLine(dataToSend[1]);
        }
    }

    public sealed class SerialComm : SerialPort
    {
        public event EventHandler OnPortClosed;
        public event EventHandler OnPortOpened;
        public event EventHandler OnPortFailed;
        public event EventHandler OnNewLineArrived;

        private string incomingData = "";

        public SerialComm()
        {
            
        }

        //<summary>
        //  Propiedad que devuelve el string de los datos recibidos.
        //</summary>
        public string GetIncomingData
        {
            get
            {
                return incomingData;
            }
        }

        //<summary>
        //  Configura el puero serie.
        //</summary>
        public void Config(int baudRate, string port)
        {
            base.BaudRate = baudRate;
            base.PortName = port;
        }
        //<summary>
        //  Abre el puerto serie. Si no hay error, lanza el evento 'OnPortOpened'. Caso contrario, se dispara el evento 'OnPortFailed'.
        //</summary>
        public new void Open()
        {
            try
            {
                base.Open();
                OnPortOpened(this, EventArgs.Empty);
            }catch (Exception)
            {
                OnPortFailed(this, EventArgs.Empty);
            }
        }
        //<summary>
        //  Cierra el puerto serie. Si no hay error, lanza el evento 'OnPortClosed'. Caso contrario, se dispara el evento 'OnPortFailed'.
        //</summary>
        public new void Close()
        {
            try
            {
                base.Close();
                OnPortClosed(this, EventArgs.Empty);
            }catch (Exception)
            {
                OnPortFailed(this, EventArgs.Empty);
            }
        }
        //<summary>
        //  Genera una tarea asíncrona para la transmisión de datos. En caso de error en la comunicación, lanza un evento 'OnPortFailed'.
        //</summary>
        public void AsyncWriteLine(string data)
        {
            new Task(new Action( () => {
                try
                {
                    WriteLine(data);
                }catch (Exception)
                {
                    OnPortFailed(this, EventArgs.Empty);
                }
            })).Start();
        }
        //<summary>
        //  Genera una tarea asíncrona para la recepción de datos. Lanza un evento 'OnNewLineArrived' cuando detecta un final de línea.
        //</summary>
        public void AsyncReadLine()
        {
            new Task(new Action(() => {
                try
                {
                    incomingData = ReadLine();
                    OnNewLineArrived(this, EventArgs.Empty);
                }
                catch (Exception)
                {
                    //OnPortFailed(this, EventArgs.Empty);
                }
            })).Start();
        }
    }

    public static class RandomData
    {
        private static Random randomByte = new Random();
        private static Color randomColor;

        private const string STRING_FORMAT = "{0:D3}-{1:D3},{2:D3},{3:D3},{4:D3}-{5:D3},{6:D3},{7:D3},{8:D3}-{9:D3}";
        private const int MAX_VALUE = Byte.MaxValue;

        enum EffectCode : byte { 
            Effect_One = 1,
            Effect_Two
        }

        //<summary>
        //  Obtiene un color aleatorio.
        //</summary>
        public static Color GetRandomColor()
        {
            byte red, green, blue;

            red = (byte)randomByte.Next(0, MAX_VALUE);
            green = (byte)randomByte.Next(0, MAX_VALUE);
            blue = (byte)randomByte.Next(0, MAX_VALUE);

            randomColor = Color.FromArgb(red, green, blue);
            return randomColor;
        }
        //<summary>
        //  Obtiene un string formateado con los colores aleatorios.
        //</summary>
        public static string[] GetRandomData()
        {
            string[] primSec = new string[2];

            Color primary = GetRandomColor();
            Color secondary = GetRandomColor();

            primSec[0] = String.Format(STRING_FORMAT,
                (byte)EffectCode.Effect_One, primary.R, primary.G, primary.B, 50, secondary.R, secondary.G, secondary.B, 50, 1);

            primary = GetRandomColor();
            secondary = GetRandomColor();

            primSec[1] = String.Format(STRING_FORMAT,
                (byte)EffectCode.Effect_Two, primary.R, primary.G, primary.B, 50, secondary.R, secondary.G, secondary.B, 50, 1);
            return primSec;
        }
    }
}
