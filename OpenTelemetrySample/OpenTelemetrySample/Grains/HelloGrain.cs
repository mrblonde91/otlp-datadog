public class HelloGrain : IHelloGrain
{
    public Task<string> SayHello(string greeting)
    {
        return Task.FromResult($"You said: '{greeting}', I say: Hello!");
    }
}