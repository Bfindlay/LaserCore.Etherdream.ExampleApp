using System;
using System.Threading;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LaserCore.Etherdream.Net.Discovery;
using LaserCore.Etherdream.Net.Device;
using LaserCore.Etherdream.Net.Dto;
using laserCore.Ilda.Net;

namespace EtherDream.Net.ExampleApp
{
    public partial class Form1 : Form
    {
        private Dac etherDream;
        private string laserFilePath = string.Empty;
        private CancellationTokenSource cancellationToken;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Create a device discovery opject
            var discovery = new DeviceDiscovery();
            // find the dac identity of the first available device
            // NOTE v 1.0.0 of LaserCore.Etherdream will throw an exception if the 
            // Dac was not already plugged in when the program was started so ensure its already on
            var dac = discovery.FindFirstDevice();
            // Create etherdream instance with the IP we found
            etherDream = new Dac(dac.Ip);
            this.DevName.Text = etherDream.IP;
        }

        private void StartLaserButton(object sender, EventArgs e)
        {

            if (string.IsNullOrEmpty(laserFilePath))
            {
                MessageBox.Show("No File Selected");
                return;
            }
            using var fileStream = new FileStream(laserFilePath, FileMode.Open);

            // Use lasercore ilda parser to parse ilda into a list of points
            var ildaPoints = IldaImporter.ParseIlda(fileStream);

            // The Ilda parser outputs just a standard point, we need to map it to 
            // the dacpoint that the etherdream is expecting
            var laserDacPoints = ildaPoints.Select(point => new DacPointDto
            {
                X = point.X,
                Y = point.Y,
                R = point.R,
                G = point.G,
                B = point.B,
                I = point.I,
                Control = 0,
                U1 = 0,
                U2 = 0
            }).ToArray();

            cancellationToken = new CancellationTokenSource();
            // prepare the device,
            // this is actually done in the streampoints method underthe hood so i need to revise this
            etherDream.Prepare();
            // run the device in a new thread to prevent GUI blocking
            Task.Run(() => etherDream.StreamPoints(laserDacPoints, 12000), cancellationToken.Token);
        }


        private void loadFile(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "ILDA Files (*.ild)|*.ild";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    laserFilePath = openFileDialog.FileName;
                    this.FileName.Text = laserFilePath;
                }
            }

        }

        private void stop(object sender, EventArgs e)
        {
            // stop the etherdream from outputting the laser
            etherDream.Stop();
            // cancel the background task
            if (cancellationToken != null)
                cancellationToken.Cancel();

        }
    }
}
