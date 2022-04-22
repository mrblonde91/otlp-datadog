// See https://aka.ms/new-console-template for more information
using OpenTelemetrySample.Contracts;
Console.WriteLine("Hello, World!");

// Make an api call
var client = new HttpClient();
var response = await client.GetAsync($"https://localhost:5000/{Endpoints.GetSimpleApiCall}");
