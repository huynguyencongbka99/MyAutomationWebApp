using System;
using System.Collections.Generic;
using System.Linq;
using AutoTechHub.Models;
using AutoTechHub.Services;

namespace AutoTechHub.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context, IMarkdownService markdownService)
        {
            context.Database.EnsureCreated();

            // Look for any categories
            if (context.Categories.Any())
            {
                return; // DB has been seeded
            }

            // 1. Seed Categories
            var categories = new List<Category>
            {
                new Category
                {
                    Name = "Tự động hóa Công nghiệp",
                    Slug = "industrial-automation",
                    Description = "PLC Siemens, Mitsubishi, Omron, SCADA, HMI, Modbus RTU/TCP, OPC UA và mạng truyền thông công nghiệp.",
                    Icon = "bi-gear-wide-connected",
                    BadgeColor = "primary",
                    DisplayOrder = 1
                },
                new Category
                {
                    Name = "Tự động hóa Phần mềm & RPA",
                    Slug = "software-rpa",
                    Description = "Playwright, Selenium, C# Bot, Python Scripts, tự động hóa quy trình nghiệp vụ và thu thập dữ liệu web.",
                    Icon = "bi-robot",
                    BadgeColor = "success",
                    DisplayOrder = 2
                },
                new Category
                {
                    Name = "IoT & Hệ thống Nhúng",
                    Slug = "iot-embedded",
                    Description = "ESP32, STM32, Arduino, Raspberry Pi, MQTT Broker, Dashboard giám sát thời gian thực và cảm biến.",
                    Icon = "bi-cpu",
                    BadgeColor = "warning",
                    DisplayOrder = 3
                },
                new Category
                {
                    Name = "DevOps & CI/CD",
                    Slug = "devops-cicd",
                    Description = "Docker, GitHub Actions, Jenkins, tự động hóa đóng gói, kiểm thử và triển khai ứng dụng.",
                    Icon = "bi-terminal-split",
                    BadgeColor = "info",
                    DisplayOrder = 4
                },
                new Category
                {
                    Name = "AI & Thị giác Máy tính",
                    Slug = "ai-vision",
                    Description = "OpenCV, YOLO, AI Agents và các giải pháp trí tuệ nhân tạo nhận diện sản phẩm trong dây chuyền sản xuất.",
                    Icon = "bi-eye",
                    BadgeColor = "danger",
                    DisplayOrder = 5
                }
            };

            context.Categories.AddRange(categories);
            context.SaveChanges();

            // 2. Seed Tags
            var tags = new List<Tag>
            {
                new Tag { Name = "PLC Siemens", Slug = "plc-siemens" },
                new Tag { Name = "S7-1200", Slug = "s7-1200" },
                new Tag { Name = "Modbus TCP", Slug = "modbus-tcp" },
                new Tag { Name = "C# .NET", Slug = "csharp-dotnet" },
                new Tag { Name = "Playwright", Slug = "playwright" },
                new Tag { Name = "RPA", Slug = "rpa" },
                new Tag { Name = "ESP32", Slug = "esp32" },
                new Tag { Name = "MQTT", Slug = "mqtt" },
                new Tag { Name = "Python", Slug = "python" },
                new Tag { Name = "OpenCV", Slug = "opencv" },
                new Tag { Name = "Docker", Slug = "docker" },
                new Tag { Name = "CI/CD", Slug = "cicd" }
            };

            context.Tags.AddRange(tags);
            context.SaveChanges();

            var tagMap = tags.ToDictionary(t => t.Slug, t => t);

            // 3. Seed Articles
            var catIndustrial = categories.First(c => c.Slug == "industrial-automation");
            var catRpa = categories.First(c => c.Slug == "software-rpa");
            var catIot = categories.First(c => c.Slug == "iot-embedded");
            var catDevOps = categories.First(c => c.Slug == "devops-cicd");
            var catAiVision = categories.First(c => c.Slug == "ai-vision");

            var article1Content = @"# Hướng Dẫn Lập Trình Giao Tiếp PLC Siemens S7-1200 Bằng C# .NET Qua S7.Net

Trong các hệ thống tự động hóa công nghiệp hiện đại, việc thu thập dữ liệu từ PLC (Programmable Logic Controller) lên máy tính cấp SCADA/MES hoặc Web Dashboard là một yêu cầu cực kỳ phổ biến. Bài viết này sẽ hướng dẫn bạn từng bước cách kết nối và đọc/ghi dữ liệu từ **PLC Siemens S7-1200** bằng ngôn ngữ **C# .NET** sử dụng thư viện mã nguồn mở **S7.Net Plus**.

---

## 1. Chuẩn Bị & Cấu Hình Trên TIA Portal

Trước khi lập trình C#, bạn cần bật chế độ cho phép truy cập PUT/GET trên PLC S7-1200 trong TIA Portal:

1. Mở phần cứng PLC trong **TIA Portal** -> Nhấp đúp vào PLC -> Chọn tab **Properties**.
2. Tìm đến mục **Protection & Security** -> **Connection mechanisms**.
3. Tích chọn vào ô: **""Permit access with PUT/GET communication from remote partner""**.
4. Đối với Data Block (DB) mà bạn muốn đọc/ghi, nhấp chuột phải vào DB -> **Properties** -> Tab **Attributes** -> **Bỏ chọn** mục *""Optimized block access""* (chuyển sang dạng Non-optimized Standard DB để có địa chỉ Offset cố định).
5. Compile và Download lại xuống PLC.

> [!IMPORTANT]
> Nếu bạn không tắt *Optimized block access* hoặc quên tích *Permit access with PUT/GET*, C# sẽ báo lỗi Timeout hoặc Connection Refused khi cố gắng đọc dữ liệu!

---

## 2. Cài Đặt Thư Viện S7.Net Trên .NET

Mở Terminal của dự án .NET và cài đặt gói NuGet:

```bash
dotnet add package S7netplus
```

---

## 3. Viết Mã Nguồn C# Kết Nối PLC

Dưới đây là lớp `SiemensPlcManager` được thiết kế theo dạng dùng async/await và quản lý kết nối an toàn:

```csharp
using System;
using System.Threading.Tasks;
using S7.Net;

public class SiemensPlcManager : IDisposable
{
    private readonly Plc _plc;

    public SiemensPlcManager(string ipAddress, CpuType cpu = CpuType.S71200, short rack = 0, short slot = 1)
    {
        _plc = new Plc(cpu, ipAddress, rack, slot);
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            await _plc.OpenAsync();
            Console.WriteLine($""[SUCCESS] Đã kết nối thành công tới PLC Siemens: {_plc.IP}"");
            return _plc.IsConnected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($""[ERROR] Lỗi kết nối PLC: {ex.Message}"");
            return false;
        }
    }

    // Đọc một giá trị Real (Float) từ DB1 tại byte offset 0 (DB1.DBD0)
    public async Task<float?> ReadTemperatureAsync(int dbNumber = 1, int startByteAdr = 0)
    {
        if (!_plc.IsConnected) return null;

        try
        {
            // S7-1200 Real = 4 bytes (Float)
            var result = await _plc.ReadAsync(DataType.DataBlock, dbNumber, startByteAdr, VarType.Real, 1);
            return (float)result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($""Lỗi đọc dữ liệu: {ex.Message}"");
            return null;
        }
    }

    // Ghi một giá trị Boolean vào biến cờ hoặc DataBlock (DB1.DBX4.0)
    public async Task<bool> SetMotorRunStatusAsync(int dbNumber, int byteAdr, int bitAdr, bool value)
    {
        if (!_plc.IsConnected) return false;

        try
        {
            await _plc.WriteBitAsync(DataType.DataBlock, dbNumber, byteAdr, bitAdr, value);
            Console.WriteLine($""Đã ghi trạng thái Motor = {value} vào DB{dbNumber}.DBX{byteAdr}.{bitAdr}"");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($""Lỗi ghi dữ liệu: {ex.Message}"");
            return false;
        }
    }

    public void Dispose()
    {
        if (_plc != null && _plc.IsConnected)
        {
            _plc.Close();
        }
    }
}
```

---

## 4. Cách Sử Dụng Trong Ứng Dụng Console Hoặc Web API

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        using var plc = new SiemensPlcManager(""192.168.0.10"");
        
        if (await plc.ConnectAsync())
        {
            // Đọc nhiệt độ lò nhiệt từ DB1.DBD0
            var temp = await plc.ReadTemperatureAsync(dbNumber: 1, startByteAdr: 0);
            Console.WriteLine($""Nhiệt độ hiện tại: {temp:F2} °C"");

            // Kích hoạt biến chạy bơm giải nhiệt DB1.DBX4.0 = true
            if (temp > 75.0f)
            {
                await plc.SetMotorRunStatusAsync(dbNumber: 1, byteAdr: 4, bitAdr: 0, value: true);
            }
        }
    }
}
```

---

## 5. Kinh Nghiệm Thực Tế Khi Triển Khai Trong Nhà Máy

- **Polling Rate hợp lý**: Không nên đọc vòng lặp với chu kỳ dưới 50ms nếu hệ thống mạng Ethernet qua nhiều switch công nghiệp không chuyên dụng. Chu kỳ 100ms - 500ms là tối ưu cho việc giám sát SCADA/Web.
- **Xử lý mất kết nối (Auto Reconnect)**: Luôn kiểm tra thuộc tính `IsConnected` và có cơ chế `Heartbeat` tự động kết nối lại sau mỗi 3 - 5 giây nếu đứt cáp mạng.
- **Đọc hàng loạt (Bulk Read)**: Thay vì đọc từng biến đơn lẻ, hãy gom các biến vào một `Class` hoặc `Struct` và dùng `_plc.ReadClassAsync(myObject, dbNumber)` để đọc 1 lần toàn bộ dữ liệu chỉ trong một frame TCP.

Chúc các bạn tích hợp thành công PLC Siemens vào ứng dụng .NET!";

            var article2Content = @"# Tự Động Hóa Web & Thu Thập Dữ Liệu Bằng Microsoft Playwright Với C# .NET

Trong thế giới tự động hóa phần mềm (Software Automation) và kiểm thử chất lượng (QA), **Microsoft Playwright** đang dần trở thành tiêu chuẩn mới thay thế Selenium nhờ tốc độ vượt trội, cơ chế Auto-waiting thông minh và khả năng hỗ trợ đa trình duyệt (Chromium, Firefox, WebKit) nguyên bản.

Bài viết này sẽ hướng dẫn bạn cách xây dựng một ứng dụng C# .NET tự động hóa các tác vụ lặp đi lặp lại trên trình duyệt và xuất báo cáo tự động.

---

## 1. Tại Sao Nên Dùng Playwright Thay Vì Selenium?

| Tính năng | Playwright | Selenium truyền thống |
| :--- | :--- | :--- |
| **Auto-wait** | Tự động chờ phần tử hiển thị / sẵn sàng click | Phải dùng `WebDriverWait` thủ công |
| **Tốc độ thực thi** | Rất nhanh nhờ kiến trúc WebSocket & DevTools Protocol | Chậm hơn do qua trung gian WebDriver HTTP |
| **Chụp ảnh & Video** | Tích hợp sẵn tracing, screenshot, video recording | Cần cấu hình phức tạp |
| **Hỗ trợ Headless** | Cực kỳ mượt mà, hỗ trợ cả browser context cô lập | Tốn tài nguyên hơn |

---

## 2. Khởi Tạo Dự Án & Cài Đặt Package

Tạo một project .NET Console:

```bash
dotnet new console -n WebAutomationBot
cd WebAutomationBot
dotnet add package Microsoft.Playwright
dotnet build
```

Sau khi build, hãy cài đặt các trình duyệt đi kèm của Playwright:

```powershell
powershell -Command ""& bin/Debug/net5.0/playwright.ps1 install""
```

---

## 3. Kịch Bản Tự Động Hóa Đăng Nhập & Tải Báo Cáo

Dưới đây là đoạn mã hoàn chỉnh thực hiện:
1. Mở trình duyệt Chromium.
2. Điều hướng đến trang quản trị nội bộ.
3. Tự động điền tài khoản, mật khẩu và đăng nhập.
4. Chờ trang tải, lọc báo cáo sản xuất theo ngày hôm nay.
5. Chụp ảnh màn hình bằng chứng và trích xuất dữ liệu bảng về dạng CSV.

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright;

class WebBot
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine(""Khởi động Playwright Web Automation Bot..."");

        using var playwright = await Playwright.CreateAsync();
        
        // Khởi động Chromium (Headless = false để quan sát giao diện)
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            SlowMo = 300 // Làm chậm 300ms giữa các thao tác để dễ quan sát
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        var page = await context.NewPageAsync();

        // 1. Điều hướng tới trang web
        Console.WriteLine(""Điều hướng tới trang hệ thống..."");
        await page.GotoAsync(""https://quotes.toscrape.com/login"");

        // 2. Điền form đăng nhập với cơ chế tự động tìm selector
        Console.WriteLine(""Điền thông tin đăng nhập..."");
        await page.FillAsync(""input#username"", ""automation_engineer"");
        await page.FillAsync(""input#password"", ""SecurePassword123!"");

        // 3. Click nút đăng nhập và chờ trang chuyển đổi
        await page.ClickAsync(""input[type='submit']"");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 4. Chụp ảnh màn hình làm bằng chứng
        Directory.CreateDirectory(""Screenshots"");
        var screenshotPath = Path.Combine(""Screenshots"", $""dashboard_{DateTime.Now:yyyyMMdd_HHmmss}.png"");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
        Console.WriteLine($""Đã lưu ảnh chụp màn hình tại: {screenshotPath}"");

        // 5. Trích xuất dữ liệu danh sách trích dẫn / đơn hàng
        var items = await page.QuerySelectorAllAsync("".quote"");
        Console.WriteLine($""Tìm thấy {items.Count} mục dữ liệu trên trang."");

        foreach (var item in items)
        {
            var textElement = await item.QuerySelectorAsync("".text"");
            var authorElement = await item.QuerySelectorAsync("".author"");

            var text = textElement != null ? await textElement.InnerTextAsync() : """";
            var author = authorElement != null ? await authorElement.InnerTextAsync() : """";

            Console.WriteLine($""- [{author}]: {text}"");
        }

        Console.WriteLine(""Hoàn thành kịch bản tự động hóa!"");
    }
}
```

---

## 4. Tự Động Hóa Với Headless Chạy Định Kỳ (Cron Job)

Khi triển khai lên Server hoặc Docker Container, bạn chỉ cần chuyển cấu hình:

```csharp
Headless = true
```

Và kết hợp với **Windows Task Scheduler** hoặc **Worker Service** để bot tự động chạy mỗi sáng lúc 06:00 AM, lấy số liệu sản xuất từ phần mềm ERP và gửi email báo cáo cho ban giám đốc.

Một giải pháp tiết kiệm hàng chục giờ nhập liệu thủ công mỗi tháng!";

            var article3Content = @"# Xây Dựng Hệ Thống IoT Giám Sát Nhiệt Độ Nhà Xưởng Với ESP32, MQTT Broker Và ASP.NET Core

Giám sát điều kiện môi trường như nhiệt độ, độ ẩm, áp suất trong nhà máy và kho lạnh là một bài toán then chốt trong tự động hóa công nghiệp và nông nghiệp công nghệ cao. Trong bài viết này, chúng ta sẽ xây dựng một giải pháp IoT hoàn chỉnh khép kín từ phần cứng **ESP32**, truyền thông qua **MQTT Broker (Mosquitto)** và hiển thị lên **Web Dashboard bằng ASP.NET Core**.

---

## 1. Sơ Đồ Kiến Trúc Hệ Thống (Architecture)

```
+-------------------+        WiFi / MQTT         +------------------+
|   ESP32 Node      | ------------------------> |   Mosquitto      |
| (Sensor DHT22/DS) |   Topic: factory/temp1    |   MQTT Broker    |
+-------------------+                           +------------------+
                                                         |
                                                  MQTT Subscriber
                                                         v
                                                +------------------+
                                                |  ASP.NET Core    |
                                                |  Backend & UI    |
                                                | (SignalR WebApp) |
                                                +------------------+
```

---

## 2. Lập Trình Firmware ESP32 (Arduino C++)

Đoạn mã dưới đây nạp vào vi điều khiển **ESP32** để đọc cảm biến và gửi gói tin JSON định kỳ 2 giây/lần:

```cpp
#include <WiFi.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include ""DHT.h""

#define DHTPIN 4
#define DHTTYPE DHT22
DHT dht(DHTPIN, DHTTYPE);

const char* ssid = ""Factory_WiFi_AP"";
const char* password = ""IndustrialPass2026"";
const char* mqtt_server = ""192.168.1.100"";
const int mqtt_port = 1883;

WiFiClient espClient;
PubSubClient client(espClient);
unsigned long lastMsg = 0;

void setup_wifi() {
  delay(10);
  Serial.println(""Connecting to WiFi..."");
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(""."");
  }
  Serial.println(""\nWiFi connected. IP: "");
  Serial.println(WiFi.localIP());
}

void reconnect() {
  while (!client.connected()) {
    Serial.print(""Connecting to MQTT broker..."");
    if (client.connect(""ESP32_Node_ZoneA"")) {
      Serial.println(""Connected!"");
    } else {
      Serial.print(""Failed, rc="");
      Serial.print(client.state());
      delay(5000);
    }
  }
}

void setup() {
  Serial.begin(115200);
  dht.begin();
  setup_wifi();
  client.setServer(mqtt_server, mqtt_port);
}

void loop() {
  if (!client.connected()) {
    reconnect();
  }
  client.loop();

  unsigned long now = millis();
  if (now - lastMsg > 3000) { // Gửi mỗi 3 giây
    lastMsg = now;
    
    float h = dht.readHumidity();
    float t = dht.readTemperature();

    if (isnan(h) || isnan(t)) {
      Serial.println(""Lỗi đọc cảm biến DHT22!"");
      return;
    }

    StaticJsonDocument<200> doc;
    doc[""deviceId""] = ""ESP32-ZONE-A1"";
    doc[""temperature""] = t;
    doc[""humidity""] = h;
    doc[""rssi""] = WiFi.RSSI();

    char buffer[256];
    serializeJson(doc, buffer);
    
    client.publish(""factory/sensors/zone-a"", buffer);
    Serial.printf(""Published: Temp=%.2f C, Hum=%.2f %%\n"", t, h);
  }
}
```

---

## 3. Xây Dựng Background Service Nhận MQTT Trong ASP.NET Core

Sử dụng thư viện `MQTTnet` trong C# để duy trì kết nối lắng nghe:

```bash
dotnet add package MQTTnet
```

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;

public class MqttConsumerService : BackgroundService
{
    private IMqttClient _mqttClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(""192.168.1.100"", 1883)
            .WithClientId(""AspNetCore_Telemetry_Listener"")
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            
            Console.WriteLine($""[MQTT NHẬN ĐƯỢC] Topic: {topic} | Data: {payload}"");
            
            // Xử lý lưu database hoặc push thời gian thực lên SignalR Web Dashboard
            return Task.CompletedTask;
        };

        await _mqttClient.ConnectAsync(options, stoppingToken);
        await _mqttClient.SubscribeAsync(""factory/sensors/#"");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

---

## 4. Kết Luận

Với kiến trúc này, hệ thống có thể mở rộng từ 1 cảm biến lên hàng trăm thiết bị IoT trong nhà máy một cách tin cậy, chi phí phần cứng thấp và quản lý tập trung trên nền tảng .NET!";

            var article4Content = @"# Ứng Dụng Computer Vision (OpenCV) Để Kiểm Tra Lỗi Sản Phẩm Trên Băng Chuyền Tự Động

Trong dây chuyền sản xuất tự động hóa, khâu kiểm tra ngoại quan (Visual Quality Inspection) đóng vai trò sống còn để loại bỏ sản phẩm lỗi trước khi đóng gói. Trước đây việc này cần nhân công soi lỗi thủ công, dễ mỏi mắt và sai sót.

Bài viết này giới thiệu cách sử dụng **OpenCV** kết hợp cùng thuật toán lọc ngưỡng màu sắc và phân tích Contour để phát hiện nắp chai bị kênh hoặc thiếu nắp trên băng chuyền.

---

## 1. Nguyên Lý Xử Lý Ảnh Công Nghiệp

Quy trình phát hiện cơ bản bao gồm 5 bước:
1. **Thu nhận ảnh (Image Acquisition)**: Camera công nghiệp chụp khi cảm biến quang phát hiện chai đến vị trí.
2. **Tiền xử lý (Preprocessing)**: Chuyển ảnh sang không gian màu HSV và làm mịn với Gaussian Blur để khử nhiễu ánh sáng.
3. **Lọc mặt nạ (Color Masking / Thresholding)**: Tách riêng vùng màu của nắp chai (ví dụ: nắp màu đỏ hoặc xanh dương).
4. **Tìm Contour (Contour Detection)**: Xác định diện tích và tọa độ tâm `(x, y)` của nắp.
5. **Đưa ra quyết định (Decision & PLC Trigger)**: Nếu diện tích `< Threshold` hoặc lệch tọa độ `> 10px`, gửi tín hiệu kích hoạt xi lanh khí nén thổi chai lỗi ra khỏi băng chuyền!

---

## 2. Mã Nguồn Xử Lý Bằng Python & OpenCV

```python
import cv2
import numpy as np

def inspect_bottle_cap(frame):
    # 1. Chuyển đổi sang không gian màu HSV
    hsv = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)

    # 2. Dải màu nắp chai màu đỏ
    lower_red1 = np.array([0, 100, 100])
    upper_red1 = np.array([10, 255, 255])
    lower_red2 = np.array([160, 100, 100])
    upper_red2 = np.array([179, 255, 255])

    mask1 = cv2.inRange(hsv, lower_red1, upper_red1)
    mask2 = cv2.inRange(hsv, lower_red2, upper_red2)
    mask = cv2.bitwise_or(mask1, mask2)

    # Khử nhiễu
    kernel = np.ones((5, 5), np.uint8)
    mask = cv2.morphologyEx(mask, cv2.MORPH_OPEN, kernel)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, kernel)

    # 3. Tìm các đường bao (Contours)
    contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

    passed = False
    details = ""Không tìm thấy nắp chai (NG - FAIL)""

    for cnt in contours:
        area = cv2.contourArea(cnt)
        if area > 1200: # Diện tích chuẩn của nắp chai
            x, y, w, h = cv2.boundingRect(cnt)
            aspect_ratio = float(w) / h
            
            # Tỷ lệ dài/rộng của nắp chuẩn khoảng 1.8 - 2.5
            if 1.5 < aspect_ratio < 3.0:
                passed = True
                details = f""Nắp chuẩn (OK) - Diện tích: {area:.0f}px""
                cv2.rectangle(frame, (x, y), (x + w, y + h), (0, 255, 0), 2)
            else:
                details = f""Nắp bị kênh/nghiêng (NG) - Aspect: {aspect_ratio:.2f}""
                cv2.rectangle(frame, (x, y), (x + w, y + h), (0, 0, 255), 2)
            break

    # Vẽ kết quả lên hình ảnh
    status_text = ""PASS [OK]"" if passed else ""REJECT [NG]""
    color = (0, 255, 0) if passed else (0, 0, 255)
    cv2.putText(frame, status_text, (20, 50), cv2.FONT_HERSHEY_SIMPLEX, 1.2, color, 3)
    cv2.putText(frame, details, (20, 90), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)

    return frame, passed

# Chạy thử nghiệm với Camera
cap = cv2.VideoCapture(0)
while cap.isOpened():
    ret, frame = cap.read()
    if not ret: break

    result_frame, is_ok = inspect_bottle_cap(frame)
    cv2.imshow(""Industrial Vision Inspection"", result_frame)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()
```

---

## 3. Tích Hợp Giao Tiếp Với PLC Đẩy Sản Phẩm Lỗi

Khi hàm `inspect_bottle_cap()` trả về `is_ok = False`, hệ thống phần mềm sẽ gửi lệnh qua **Modbus TCP** hoặc **S7 protocol** tới PLC:

```csharp
if (!isOk)
{
    // Kích hoạt ngõ ra Q0.0 trong 200ms để van khí nén đẩy chai NG sang thùng phế
    await plc.TriggerEjectorPistonAsync();
}
```

Đây là mô hình tiêu chuẩn đang được áp dụng trong hàng nghìn nhà máy đóng chai và thực phẩm hiện đại!";

            var articles = new List<Article>
            {
                new Article
                {
                    Title = "Hướng Dẫn Lập Trình Giao Tiếp PLC Siemens S7-1200 Bằng C# .NET Qua S7.Net",
                    Slug = "huong-dan-lap-trinh-giao-tiep-plc-siemens-s7-1200-bang-csharp-net",
                    Summary = "Chi tiết cách kết nối, đọc thanh ghi Data Block (DB) và ghi trạng thái điều khiển PLC Siemens S7-1200 từ ứng dụng C# .NET với thư viện S7NetPlus an toàn và tốc độ cao.",
                    ContentMarkdown = article1Content,
                    ContentHtml = markdownService.ToHtml(article1Content),
                    ThumbnailUrl = "https://images.unsplash.com/photo-1581092160607-ee22621dd758?auto=format&fit=crop&w=800&q=80",
                    AuthorName = "Nguyễn Văn Tuấn (Automation Lead)",
                    AuthorEmail = "tuan.automation@example.com",
                    ReadingTimeMinutes = 6,
                    ViewCount = 342,
                    LikeCount = 48,
                    IsFeatured = true,
                    IsPublished = true,
                    CategoryId = catIndustrial.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Article
                {
                    Title = "Tự Động Hóa Web & Thu Thập Dữ Liệu Bằng Microsoft Playwright Với C# .NET",
                    Slug = "tu-dong-hoa-web-thu-thap-du-lieu-playwright-csharp-net",
                    Summary = "Khám phá cách xây dựng Bot tự động hóa quy trình nghiệp vụ (RPA) bằng Playwright & C# .NET để tự động đăng nhập, cào số liệu và xuất báo cáo không cần thao tác người dùng.",
                    ContentMarkdown = article2Content,
                    ContentHtml = markdownService.ToHtml(article2Content),
                    ThumbnailUrl = "https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=800&q=80",
                    AuthorName = "Lê Hoàng Nam (RPA Architect)",
                    AuthorEmail = "nam.le@example.com",
                    ReadingTimeMinutes = 5,
                    ViewCount = 285,
                    LikeCount = 39,
                    IsFeatured = true,
                    IsPublished = true,
                    CategoryId = catRpa.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Article
                {
                    Title = "Xây Dựng Hệ Thống IoT Giám Sát Nhiệt Độ Nhà Xưởng Với ESP32, MQTT Broker Và ASP.NET Core",
                    Slug = "xay-dung-he-thong-iot-giam-sat-nhiet-do-esp32-mqtt-aspnet-core",
                    Summary = "Hướng dẫn lập trình phần cứng ESP32 đọc cảm biến DHT22 truyền qua MQTT Broker và xây dựng Background Worker Service trên ASP.NET Core để giám sát thời gian thực.",
                    ContentMarkdown = article3Content,
                    ContentHtml = markdownService.ToHtml(article3Content),
                    ThumbnailUrl = "https://images.unsplash.com/photo-1517077304055-6e89abbf09b0?auto=format&fit=crop&w=800&q=80",
                    AuthorName = "Trần Minh Quang (IoT Specialist)",
                    AuthorEmail = "quang.tran@example.com",
                    ReadingTimeMinutes = 7,
                    ViewCount = 412,
                    LikeCount = 65,
                    IsFeatured = true,
                    IsPublished = true,
                    CategoryId = catIot.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Article
                {
                    Title = "Ứng Dụng Computer Vision (OpenCV) Để Kiểm Tra Lỗi Sản Phẩm Trên Băng Chuyền Tự Động",
                    Slug = "ung-dung-computer-vision-opencv-kiem-tra-loi-san-pham-bang-chuyen",
                    Summary = "Giải pháp kiểm tra ngoại quan (Visual Inspection) tự động bằng OpenCV, phân tích contour nắp chai và tích hợp tín hiệu điều khiển van khí nén loại bỏ sản phẩm lỗi.",
                    ContentMarkdown = article4Content,
                    ContentHtml = markdownService.ToHtml(article4Content),
                    ThumbnailUrl = "https://images.unsplash.com/photo-1531297484001-80022131f5a1?auto=format&fit=crop&w=800&q=80",
                    AuthorName = "Phạm Đức Anh (AI Vision Eng)",
                    AuthorEmail = "anh.pham@example.com",
                    ReadingTimeMinutes = 6,
                    ViewCount = 198,
                    LikeCount = 31,
                    IsFeatured = false,
                    IsPublished = true,
                    CategoryId = catAiVision.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            context.Articles.AddRange(articles);
            context.SaveChanges();

            // Link Article Tags
            var a1 = articles[0];
            var a2 = articles[1];
            var a3 = articles[2];
            var a4 = articles[3];

            context.ArticleTags.AddRange(new List<ArticleTag>
            {
                new ArticleTag { ArticleId = a1.Id, TagId = tagMap["plc-siemens"].Id },
                new ArticleTag { ArticleId = a1.Id, TagId = tagMap["s7-1200"].Id },
                new ArticleTag { ArticleId = a1.Id, TagId = tagMap["csharp-dotnet"].Id },
                new ArticleTag { ArticleId = a1.Id, TagId = tagMap["modbus-tcp"].Id },

                new ArticleTag { ArticleId = a2.Id, TagId = tagMap["playwright"].Id },
                new ArticleTag { ArticleId = a2.Id, TagId = tagMap["rpa"].Id },
                new ArticleTag { ArticleId = a2.Id, TagId = tagMap["csharp-dotnet"].Id },

                new ArticleTag { ArticleId = a3.Id, TagId = tagMap["esp32"].Id },
                new ArticleTag { ArticleId = a3.Id, TagId = tagMap["mqtt"].Id },
                new ArticleTag { ArticleId = a3.Id, TagId = tagMap["csharp-dotnet"].Id },

                new ArticleTag { ArticleId = a4.Id, TagId = tagMap["opencv"].Id },
                new ArticleTag { ArticleId = a4.Id, TagId = tagMap["python"].Id }
            });

            // 4. Seed Comments
            context.Comments.AddRange(new List<Comment>
            {
                new Comment
                {
                    ArticleId = a1.Id,
                    AuthorName = "Kỹ sư Bảo trì",
                    AuthorEmail = "maintenance@factory.vn",
                    Content = "Bài viết rất chi tiết và đúng thực tế! Phần lưu ý bỏ Optimized block access trên TIA Portal rất nhiều người mới làm hay bị mắc phải.",
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                    IsApproved = true
                },
                new Comment
                {
                    ArticleId = a1.Id,
                    AuthorName = "Hoàng Long",
                    AuthorEmail = "long.h@gmail.com",
                    Content = "Anh cho em hỏi nếu PLC đặt pass Protection thì cấu hình S7NetPlus như thế nào ạ?",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    IsApproved = true
                },
                new Comment
                {
                    ArticleId = a3.Id,
                    AuthorName = "Thanh Tùng",
                    AuthorEmail = "tung.iot@outlook.com",
                    Content = "Mô hình ESP32 + MQTT + ASP.NET Core này chạy rất ổn định trong xưởng may của mình, đã test 3 tháng liên tục không bị drop.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    IsApproved = true
                }
            });

            // 5. Seed Code Snippets
            context.CodeSnippets.AddRange(new List<CodeSnippet>
            {
                new CodeSnippet
                {
                    Title = "Đọc dữ liệu Modbus TCP Holding Registers (C# NModbus)",
                    Description = "Hàm C# kết nối Modbus TCP Master để đọc 10 thanh ghi Holding Register từ slave address 1.",
                    Language = "csharp",
                    CategoryTag = "Industrial",
                    Author = "AutoTech Team",
                    CodeContent = @"using System.Net.Sockets;
using NModbus;

public async Task<ushort[]> ReadModbusRegistersAsync(string ip, int port = 502, byte slaveId = 1, ushort startAddress = 0, ushort numRegisters = 10)
{
    using var client = new TcpClient();
    await client.ConnectAsync(ip, port);
    
    var factory = new ModbusFactory();
    var master = factory.CreateMaster(client);
    
    // Đọc Holding Registers (Function Code 03)
    ushort[] registers = await master.ReadHoldingRegistersAsync(slaveId, startAddress, numRegisters);
    return registers;
}",
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                },
                new CodeSnippet
                {
                    Title = "ESP32 Auto Reconnect WiFi & MQTT Keepalive (C++)",
                    Description = "Hàm chuẩn kiểm tra trạng thái WiFi và tự động kết nối lại không làm treo tác vụ điều khiển.",
                    Language = "cpp",
                    CategoryTag = "IoT",
                    Author = "IoT Maker",
                    CodeContent = @"void checkAndReconnectNetwork() {
  if (WiFi.status() != WL_CONNECTED) {
    Serial.println(""WiFi mất kết nối, đang thử lại..."");
    WiFi.disconnect();
    WiFi.reconnect();
    int timeout = 0;
    while (WiFi.status() != WL_CONNECTED && timeout < 20) {
      delay(500);
      timeout++;
    }
  }

  if (WiFi.status() == WL_CONNECTED && !mqttClient.connected()) {
    if (mqttClient.connect(""ESP32_AutoNode"")) {
      mqttClient.subscribe(""command/control/#"");
    }
  }
}",
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new CodeSnippet
                {
                    Title = "Python Watchdog Giám Sát Thư Mục & Tự Động Xử Lý File (Python)",
                    Description = "Script tự động lắng nghe khi có file Excel/CSV mới xuất từ máy CNC/PLC và kích hoạt xử lý.",
                    Language = "python",
                    CategoryTag = "RPA",
                    Author = "DevOps Auto",
                    CodeContent = @"import time
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

class ProductionFileHandler(FileSystemEventHandler):
    def on_created(self, event):
        if not event.is_directory and event.src_path.endswith('.csv'):
            print(f""Phát hiện file dữ liệu mới: {event.src_path}"")
            # Kích hoạt xử lý số liệu tự động
            process_production_log(event.src_path)

def process_production_log(filepath):
    print(f""Đang phân tích dữ liệu từ {filepath}..."")

if __name__ == ""__main__"":
    observer = Observer()
    observer.schedule(ProductionFileHandler(), path=""./ProductionLogs"", recursive=False)
    observer.start()
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        observer.stop()
    observer.join()",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            });

            context.SaveChanges();
        }
    }
}
