## RedFoxMQ

RedFoxMQ is a .NET in-memory message queue that uses a simple TCP transport. It is fairly lightweight
and provides full control over message serialization / de-serialization. The performance is very
good (broadcasting messages over TCP reaches 300k IOPS on my machine).

#### Supported Features

- easy integration (no installation needed)
- Publisher / Subscriber scenario
- Request / Response scenario
- TCP / InProc transport
- message batching

#### Planned Features

- ServiceBus implementation
- shared memory transport for faster inter-process communication

Also there are still a lot of bugs. Not recommended for production use yet!

#### Unsupported Features

- message persistence
- message timestamps
- unique message IDs
- reliable transport
- encryption

The features above are not going to be implemented to keep the message queue 
lightweight. But there are ways around it. You could implement some these features 
within your specific use case (e.g. just add timestamps to your messages).

#### Usage Example

The easiest way is to look at the unit tests. They are a good source of examples, e.g.:

    [TestFixture]
    public class RequestResponderTests
    {
        [Test]
        public void Request_Response_single_message()
        {
            var workUnitFactory = new ResponderWorkUnitFactory(request => new TestResponderWorkUnit(request));
            using (var responder = new Responder(workUnitFactory))
            using (var requester = new Requester())
            {
                var endpoint = new RedFoxEndpoint(RedFoxTransport.Tcp, "localhost", 5555, null);
                responder.Bind(endpoint);

                requester.Connect(endpoint);

                var requestMessage = new TestMessage { Text = "Hello" };
                var responseMessage = (TestMessage)requester.Request(requestMessage);

                Assert.AreEqual(requestMessage.Text, responseMessage.Text);
            }
        }

        [SetUp]
        public void Setup()
        {
            MessageSerialization.Instance.RegisterSerializer(new TestMessage().MessageTypeId, new TestMessageSerializer());
            MessageSerialization.Instance.RegisterDeserializer(new TestMessage().MessageTypeId, new TestMessageDeserializer());
        }
    }
    
Work unit class (which creates responses):

    class TestResponderWorkUnit : IResponderWorkUnit
    {
        private readonly IMessage _request;

        public ResponderWorkUnit(IMessage request)
        {
            _request = request;
        }

        public IMessage GetResponse(object state)
        {
            // echo request -> response
            var response = _request; 
            // or create a different response here
            return response;
        }
    }

The message serialization / deserialization are like:

    class TestMessageSerializer : IMessageSerializer
    {
        public byte[] Serialize(IMessage message)
        {
            var testMessage = (TestMessage) message;
            return Encoding.UTF8.GetBytes(testMessage.Text);
        }
    }

    class TestMessageDeserializer : IMessageDeserializer
    {
        public IMessage Deserialize(byte[] rawMessage)
        {
            return new TestMessage { Text = Encoding.UTF8.GetString(rawMessage) };
        }
    }

    class TestMessage : IMessage
    {
        public ushort MessageTypeId { get; private set; }
        public string Text { get; set; }

        public TestMessage()
        {
            MessageTypeId = 1;
        }
    }    

I recommend using [Protocol Buffers](https://code.google.com/p/protobuf-net/)
for message serialization, but it is entirely up to you!

#### Contact

Please let me know if there are bugs or if you have suggestions how to improve the code.
I accept pull requests.

And maybe follow me [@quadfinity](https://twitter.com/quadfinity) :)
