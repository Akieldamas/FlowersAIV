#if WINDOWS
using AForge.Video;
using AForge.Video.DirectShow;
#endif
using System;
using System.IO;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Drawing;
using FlowersAIV.Controller;
using FlowersAIV.Model;

namespace FlowersAIV
{
    public partial class MainPage : ContentPage
    {
#if WINDOWS
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        
        private Bitmap currentFrame;
        private string currentFramePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "current_frame.jpg"); // Save path for the current frame
#endif

        LlamaService llamaService;
        PlantInfo plantInfo;
        string selectedLanguage = "French";
        public List<string> Languages { get; set; } = new List<string>
        {
            "English", "French", "Spanish", "German", "Italian"
        };

        public MainPage()
        {
            InitializeComponent();
            plantInfo = new PlantInfo();
            llamaService = new LlamaService();
            LanguagePicker.ItemsSource = Languages;
#if ANDROID
            BTN_StartCamera.IsVisible = false;
            BTN_StopCamera.IsVisible = false;
#endif
        }

        // Handle Language Selection Change
        private void OnLanguageSelected(object sender, EventArgs e)
        {
            selectedLanguage = LanguagePicker.SelectedItem?.ToString();
            DisplayAlert("Language Selected", $"You selected: {selectedLanguage}", "OK");
        }


        // Handle Camera Start
        private void OnStartCameraClicked(object sender, EventArgs e)
        {
#if WINDOWS
            try
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count == 0)
                {
                    DisplayAlert("Error", "No video devices found", "OK");
                    return;
                }

                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += OnNewFrame;
                videoSource.Start();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Error starting camera: {ex.Message}", "OK");
            }
#endif
        }
#if WINDOWS

        // Capture frame, save it to a file, and update image
        private void OnNewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                currentFrame = (Bitmap)eventArgs.Frame.Clone();

                // Save the captured frame to the currentFramePath
                currentFrame.Save(currentFramePath, System.Drawing.Imaging.ImageFormat.Png); // You can use PNG or JPEG based on your preference

                // Use the saved image file directly
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SelectedImage.Source = ImageSource.FromFile(currentFramePath);
                });
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Error capturing frame: {ex.Message}", "OK");
            }
        }
#endif


        // Handle Camera Stop
        private void OnStopCameraClicked(object sender, EventArgs e)
        {
            #if WINDOWS
            try
            {
                // Stop camera
                if (videoSource != null && videoSource.IsRunning)
                {
                    videoSource.SignalToStop();
                    videoSource.WaitForStop();
                }

                // Directly use the saved file path to make prediction
                var result = PredictImageFromFile(currentFramePath);

                // Clear image after stopping the camera
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"Error stopping camera: {ex.Message}", "OK");
            }
#endif
        }

            // Handle Image Picker Button
        private async void OnChooseImageClicked(object sender, EventArgs e)
        {
            var file = await FilePicker.PickAsync();
            if (file != null)
            {
                // Save the picked file to a local path
                var pickedFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), file.FileName);
                using (var stream = await file.OpenReadAsync())
                {
                    using (var fileStream = new FileStream(pickedFilePath, FileMode.Create))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }

                // Update the image source with the saved path
                SelectedImage.Source = ImageSource.FromFile(pickedFilePath);

                // Directly pass the file path for prediction
               PredictImageFromFile(pickedFilePath);
            }
        }

        // Simplified Prediction function that directly uses the file path and evaluates prediction score
        public async Task<MLFlowers.ModelOutput> PredictImageFromFile(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                throw new FileNotFoundException("The specified image file does not exist.");
            }

            var imageBytes = File.ReadAllBytes(imagePath);

            MLFlowers.ModelInput sampleData = new MLFlowers.ModelInput()
            {
                ImageSource = imageBytes,
            };

            var result = await Task.Run(() => MLFlowers.Predict(sampleData));

            if (result == null) throw new InvalidOperationException("Prediction result is null.");


            LB_PredictedFlower.Text = result.PredictedLabel;
            if (result != null)
            {
                plantInfo = await llamaService.GetPlantInfo(LB_PredictedFlower.Text, selectedLanguage);
                if (plantInfo != null)
                {
                    LB_Nom_ScientifiqueNom.Text = plantInfo.NameAndScientificName;
                    LB_Caracteristiques.Text = plantInfo.Characteristics;
                    LB_Description.Text = plantInfo.Description;
                }
            }

            // Optionally, return the result if further usage is needed
            return result;
        }
    }
}
