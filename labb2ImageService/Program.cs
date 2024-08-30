using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;



namespace labb2ImageService
{
    internal class Program
    {
        private static ComputerVisionClient cvClient;

        static async Task Main(string[] args)
        {
            // Set up Computer Vision client
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();
            string cogSvcEndpoint = configuration["CognitiveServicesEndpoint"];
            string cogSvcKey = configuration["CognitiveServiceKey"];

            ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(cogSvcKey);
            cvClient = new ComputerVisionClient(credentials) { Endpoint = cogSvcEndpoint };

            while (true)
            {
                Console.WriteLine("Please choose an option:\n1: Analyze image from file path\n2: Analyze image from URL\n0: Exit");
                int choice = Convert.ToInt32(Console.ReadLine());

                switch (choice)
                {
                    case 1:
                        await AnalyzeImageFromFile();
                        break;
                    case 2:
                        await AnalyzeImageFromURL();
                        break;
                    case 0:
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Invalid choice, please try again.");
                        break;
                }
            }
        }

        static async Task AnalyzeImageFromFile()
        {
            Console.WriteLine("Please enter the file path of the image:");
            string filePath = Console.ReadLine();

            if (File.Exists(filePath))
            {
                await AnalyzeImage(filePath);
            }
            else
            {
                Console.WriteLine("File not found. Please check the path and try again.");
            }
        }

        static async Task AnalyzeImageFromURL()
        {
            Console.WriteLine("Please enter the URL of the image:");
            string url = Console.ReadLine();

            // Download image to local path
            string fileName = "downloaded_image.jpg";
            string savePath = Path.Combine(Environment.CurrentDirectory, fileName);

            using (HttpClient client = new HttpClient())
            {
                byte[] imageBytes = await client.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(savePath, imageBytes);
                Console.WriteLine($"Image downloaded and saved as {savePath}");
            }

            await AnalyzeImage(savePath);
        }

        static async Task AnalyzeImage(string imageFile)
        {
            Console.WriteLine($"Analyzing {imageFile}");

            List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
            {
                VisualFeatureTypes.Description,
                VisualFeatureTypes.Tags,
                VisualFeatureTypes.Objects
            };

            using (var imageData = File.OpenRead(imageFile))
            {
                var analysis = await cvClient.AnalyzeImageInStreamAsync(imageData, features);

                Console.WriteLine("Description:");
                foreach (var caption in analysis.Description.Captions)
                {
                    Console.WriteLine($" - {caption.Text} (Confidence: {caption.Confidence:P})");
                }

                Console.WriteLine("Tags:");
                foreach (var tag in analysis.Tags)
                {
                    Console.WriteLine($" - {tag.Name} (Confidence: {tag.Confidence:P})");
                }

                if (analysis.Objects.Count > 0)
                {
                    Console.WriteLine("Objects detected:");
                    foreach (var detectedObject in analysis.Objects)
                    {
                        Console.WriteLine($" - {detectedObject.ObjectProperty} (Confidence: {detectedObject.Confidence:P})");

                        DrawBoundingBox(imageFile, detectedObject.Rectangle, detectedObject.ObjectProperty);
                    }
                }

                // Ask user if they want to create a thumbnail
                Console.WriteLine("Do you want to create a thumbnail? (y/n)");
                string createThumbnail = Console.ReadLine();

                if (createThumbnail.ToLower() == "y")
                {
                    CreateThumbnail(imageFile);
                }
            }
        }

        static void DrawBoundingBox(string imageFile, BoundingRect rect, string objectName)
        {
            using (var image = Image.FromFile(imageFile))
            {
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    Pen pen = new Pen(Color.Red, 3);
                    Font font = new Font("Arial", 16);
                    SolidBrush brush = new SolidBrush(Color.Black);

                    Rectangle boundingBox = new Rectangle(rect.X, rect.Y, rect.W, rect.H);
                    graphics.DrawRectangle(pen, boundingBox);
                    graphics.DrawString(objectName, font, brush, rect.X, rect.Y);

                    // Specify what folder image will be saved in
                    string saveDirectory = @"C:\Users\calle\source\repos\labb2ImageService\labb2ImageService\BoundingBoxes";

                    //Use this code insted if you want to try the program ur self
                    //string saveDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BoundingBoxes");

                    // Create that folder if it doesn't exsist
                    Directory.CreateDirectory(saveDirectory);

                    // Adding the combined path to save file
                    string outputFileName = Path.Combine(saveDirectory, "output_with_bounding_boxes.jpg");

                    // Saving the image
                    image.Save(outputFileName);
                    Console.WriteLine($"Output image saved as {outputFileName}");
                }
            }
        }

        static void CreateThumbnail(string imageFile)
        {
            Console.WriteLine("Enter the width of the thumbnail:");
            int width = int.Parse(Console.ReadLine());

            Console.WriteLine("Enter the height of the thumbnail:");
            int height = int.Parse(Console.ReadLine());

            Console.WriteLine("Enter the name of the thumbnail (remember to put .jpg at the end)");
            string thumbnailName = Console.ReadLine();

            // Specify what folder image will be saved in
            string saveDirectory = @"C:\Users\calle\source\repos\labb2ImageService\labb2ImageService\Thumbnails";

            //Use this code if you would like to try the program ur self
            //string saveDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Thumbnails");

            // Creating Thumbnails-folder if it doesn't exsist
            Directory.CreateDirectory(saveDirectory);

            using (var image = Image.FromFile(imageFile))
            {
                using (var thumbnail = image.GetThumbnailImage(width, height, () => false, IntPtr.Zero))
                {
                    // Saving thumbnail to that folder
                    string thumbnailFileName = Path.Combine(saveDirectory, thumbnailName);
                    thumbnail.Save(thumbnailFileName);
                    Console.WriteLine($"Thumbnail created and saved in {thumbnailFileName}");
                }
            }
        }
    }
}
