using System.Text;
using Flurl.Http.Configuration;

namespace vMixLib;

public enum VmixFunctions
{
    SetText
}

public class Vmix
{
    private const string _URL = "http://127.0.0.1:8088/API/?";
    private string _function;
    private string _input;
    private string _selectedName;
    private string _value;
    
    public string Function
    {
        get => _function;
        set => _function = $"Function={value}";
    }

    public string InputId
    {
        get => _input;
        set => _input = $"&Input={value}";
    }
    
    public string SelectedName 
    {
        get => _selectedName;
        set => _selectedName = $"&SelectedName={value}";
    }

    public string Value
    {
        get => _value;
        set => _value = $"&Value={value}";
    }

    private StringBuilder _url;

    public string UpdateUrl()
    {
        _url = new StringBuilder();
        _url.Append(_URL);
        _url.Append(Function);
        _url.Append(InputId);
        _url.Append(SelectedName);
        _url.Append(Value);
        
        return _url.ToString();
    }
    
    public static async Task SendRequest(string apiUrl)
    {
        // Create an instance of HttpClient
        using var client = new HttpClient();
        try
        {
            // Send the GET request
            HttpResponseMessage response = await client.GetAsync(apiUrl);

            // Check if the request was successful (status code 200)
            if (response.IsSuccessStatusCode)
            {
                // Read and display the response content
                string content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[{DateTime.Now}] Response Content: {content}");
            }
            else
            {
                // Handle the case where the request was not successful
                Console.WriteLine($"[{DateTime.Now}] Error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] Error: {ex.Message}");
        }
    }
}