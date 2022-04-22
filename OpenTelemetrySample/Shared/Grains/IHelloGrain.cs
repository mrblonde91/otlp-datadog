using Orleans;

public interface IHelloGrain : IGrainWithStringKey
{
    Task<string> SayHello(string greeting);
}