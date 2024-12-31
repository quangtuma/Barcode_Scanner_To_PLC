using AForge.Video;
using AForge.Video.DirectShow; // Thư viện hỗ trợ chạy webcam
using System;
using System.Drawing;
using System.Windows.Forms;
using ZXing; // Thư viện nhận dạng mã Bar Code
using S7.Net; // Thư viện PLC Siemen

namespace BarCodeScanner
{
    public partial class BarCodeScannerForm : Form
    {
        // Khai báo các biến 
        FilterInfoCollection filterInfoCollection;
        VideoCaptureDevice videoCaptureDevice;
        Plc plc;

        const string address_write = "DB30.DBW38";
        const string address_read_flag = "M7.2";

        // Lưu trữ thông tin mã Bar Code hiện tại
        string currentCode = string.Empty;

        // Hàm khởi tạo form
        public BarCodeScannerForm()
        {
            InitializeComponent();
            richTextBoxData.HideSelection = false;

            plc = new Plc(CpuType.S71200, "192.168.1.156", 0, 0);

        }

        // Hàm thực thi khi form được mở
        private void BarCodeScannerForm_Load(object sender, EventArgs e)
        {
            // lấy danh sách camera và đưa vào hộp chọn lựa
            filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo Device in filterInfoCollection)
                comboBox.Items.Add(Device.Name);
            if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;
            videoCaptureDevice = new VideoCaptureDevice();

            try
            {
                plc.Open();

                MessageBox.Show("Connect PLC Sucessfully!", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (PlcException ex)
            {
                MessageBox.Show(ex.Message, "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Hàm bắt đầu chạy nhận dạng mã Bar Code realtime
        private void buttonStart_Click(object sender, EventArgs e)
        {
            videoCaptureDevice = new VideoCaptureDevice(filterInfoCollection[comboBox.SelectedIndex].MonikerString);
            videoCaptureDevice.NewFrame += CaptureDevice_NewFrame;
            videoCaptureDevice.Start();
        }
        private void CaptureDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bm = (Bitmap)eventArgs.Frame.Clone();
            BarcodeReader reader = new BarcodeReader();
            var result = reader.Decode(bm);

            if (result != null && result.Text.Length == 3)
            {
                // hiển thị 
                if (richTextBoxData.InvokeRequired)
                {
                    richTextBoxData.Invoke(new MethodInvoker(() =>
                    {
                        richTextBoxData.AppendText(result.Text + '\n');
                    }));
                }
                else
                {
                    richTextBoxData.AppendText(result.Text + '\n');
                }

                if (result.Text == currentCode)
                {
                    return;
                }

                currentCode = result.Text;

                try
                {
                    bool res = (bool)plc.Read(address_read_flag);

                    // kiểm tra giá trị cho phép ghi
                    if (res)
                    {
                        // ghi vào PLC
                        plc.Write(address_write, Convert.ToInt16(currentCode));
                        MessageBox.Show($"Write {currentCode} to {address_write} successfully!", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex) 
                {
                    MessageBox.Show(ex.Message, "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            pictureBoxCamera.Image = bm;
        }

        // Hàm tắt các dịch vụ đang chạy (camera và PLC) khi tắt form
        private void BarCodeScannerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (videoCaptureDevice.IsRunning == true)
                videoCaptureDevice.Stop();

            plc.Close();
        }
    }
}
