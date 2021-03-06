﻿// 
// Copyright 2013-2014 Hans Wolff
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 

using NUnit.Framework;
using RedFoxMQ.Transports;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RedFoxMQ.Tests
{
    [TestFixture]
    public class RequestResponderTests
    {
        public static readonly TimeSpan Timeout = !Debugger.IsAttached ? TimeSpan.FromSeconds(10) : TimeSpan.FromMilliseconds(-1);
        
        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Request_Response_single_message(RedFoxTransport transport)
        {
            using (var responder = TestHelpers.CreateTestResponder())
            using (var requester = new Requester())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);

                responder.Bind(endpoint);

                requester.Connect(endpoint);

                Thread.Sleep(100);

                var messageSent = new TestMessage { Text = "Hello" };
                var messageReceived = (TestMessage)requester.Request(messageSent);

                Assert.AreEqual(messageSent.Text, messageReceived.Text);
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void RequestAsync_Response_single_message(RedFoxTransport transport)
        {
            using (var responder = TestHelpers.CreateTestResponder())
            using (var requester = new Requester())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);

                responder.Bind(endpoint);

                requester.Connect(endpoint);

                Thread.Sleep(100);

                var messageSent = new TestMessage { Text = "Hello" };
                var messageReceived = (TestMessage)requester.RequestAsync(messageSent).Result;

                Assert.AreEqual(messageSent.Text, messageReceived.Text);
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void RequestAsync_Cancel_single_message(RedFoxTransport transport)
        {
            using (var responder = TestHelpers.CreateTestResponder(1000))
            using (var requester = new Requester())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);

                responder.Bind(endpoint);

                requester.Connect(endpoint);

                Thread.Sleep(100);

                var messageSent = new TestMessage { Text = "Hello" };
                var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token;
                Assert.Throws<AggregateException>(() => requester.RequestAsync(messageSent, cancellationToken).Wait());
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Request_Response_two_messages(RedFoxTransport transport)
        {
            using (var responder = TestHelpers.CreateTestResponder())
            using (var requester = new Requester())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);

                responder.Bind(endpoint);
                requester.Connect(endpoint);

                var messagesReceived = new List<TestMessage>();
                Thread.Sleep(100);

                var messageSent = new TestMessage { Text = "Hello" };
                messagesReceived.Add((TestMessage)requester.Request(messageSent));
                messagesReceived.Add((TestMessage)requester.Request(messageSent));

                Assert.AreEqual(messageSent.Text, messagesReceived[0].Text);
                Assert.AreEqual(messageSent.Text, messagesReceived[1].Text);
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void RequestAsync_Response_two_messages_wait_for_each_message(RedFoxTransport transport)
        {
            using (var responder = TestHelpers.CreateTestResponder())
            using (var requester = new Requester())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);

                responder.Bind(endpoint);
                requester.Connect(endpoint);

                var messagesReceived = new List<TestMessage>();
                Thread.Sleep(100);

                var messageSent = new TestMessage { Text = "Hello" };
                messagesReceived.Add((TestMessage)requester.RequestAsync(messageSent).Result);
                messagesReceived.Add((TestMessage)requester.RequestAsync(messageSent).Result);

                Assert.AreEqual(messageSent.Text, messagesReceived[0].Text);
                Assert.AreEqual(messageSent.Text, messagesReceived[1].Text);
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void RequestAsync_Response_two_messages_simultaneous(RedFoxTransport transport)
        {
            using (var responder = TestHelpers.CreateTestResponder())
            using (var requester = new Requester())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);

                responder.Bind(endpoint);
                requester.Connect(endpoint);

                var messagesReceived = new List<TestMessage>();
                Thread.Sleep(100);

                var messageSent = new TestMessage { Text = "Hello" };
                var task1 = requester.RequestAsync(messageSent);
                var task2 = requester.RequestAsync(messageSent);
                Assert.IsTrue(Task.WhenAll(task1, task2).Wait(TimeSpan.FromSeconds(1)));

                messagesReceived.Add((TestMessage)task1.Result);
                messagesReceived.Add((TestMessage)task2.Result);

                Assert.AreEqual(messageSent.Text, messagesReceived[0].Text);
                Assert.AreEqual(messageSent.Text, messagesReceived[1].Text);
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Request_Response_different_threads_large_message(RedFoxTransport transport)
        {
            var endpoint = TestHelpers.CreateEndpointForTransport(transport);
            var started = new ManualResetEventSlim();
            var stop = new ManualResetEventSlim();

            Task.Run(() =>
            {
                using (var responder = TestHelpers.CreateTestResponder())
                {
                    responder.Bind(endpoint);
                    started.Set();
                    stop.Wait();
                }
            });

            var largeMessage = new TestMessage { Text = new string('x', 1024 * 1024) };

            TestMessage messageReceived = null;
            Task.Run(() =>
            {
                using (var requester = new Requester())
                {
                    started.Wait(Timeout);

                    requester.Connect(endpoint);

                    messageReceived = (TestMessage)requester.Request(largeMessage);

                    stop.Set();
                }
            });

            try
            {
                Assert.IsTrue(stop.Wait(Timeout));
                Assert.AreEqual(largeMessage.Text, messageReceived.Text);
            }
            finally
            {
                stop.Set();
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Responder_ClientConnected_event_fired(RedFoxTransport transport)
        {
            using (var responder = TestHelpers.CreateTestResponder())
            using (var requester = new Requester())
            {
                var eventFired = new ManualResetEventSlim();
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);

                responder.ClientConnected += (s, c) => eventFired.Set();
                responder.Bind(endpoint);

                requester.Connect(endpoint);

                Thread.Sleep(100);

                requester.Disconnect();

                Assert.IsTrue(eventFired.Wait(Timeout));
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Responder_Connects_with_timeout_ClientConnected_event_fired(RedFoxTransport transport)
        {
            using (var responder = TestHelpers.CreateTestResponder())
            using (var requester = new Requester())
            {
                var eventFired = new ManualResetEventSlim();
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);

                responder.ClientConnected += (s, c) => eventFired.Set();
                responder.Bind(endpoint);

                requester.Connect(endpoint, TimeSpan.FromSeconds(5));

                Thread.Sleep(100);

                requester.Disconnect();

                Assert.IsTrue(eventFired.Wait(Timeout));
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Requester_Disconnected_event_fired(RedFoxTransport transport)
        {
            using (var responder = TestHelpers.CreateTestResponder())
            using (var requester = new Requester())
            {
                var eventFired = new ManualResetEventSlim();
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);

                responder.Bind(endpoint);

                requester.Disconnected += eventFired.Set;
                requester.Connect(endpoint);

                Thread.Sleep(100);

                requester.Disconnect();

                Assert.IsTrue(eventFired.Wait(Timeout));
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Requester_IsDisconnected_should_be_false_when_connected(RedFoxTransport transport)
        {
            using (var responder = TestHelpers.CreateTestResponder())
            using (var requester = new Requester())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);
                responder.Bind(endpoint);
                requester.Connect(endpoint);

                Assert.IsFalse(requester.IsDisconnected);
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Requester_IsDisconnected_should_be_true_when_disconnected(RedFoxTransport transport)
        {
            using (var responder = TestHelpers.CreateTestResponder())
            using (var requester = new Requester())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);
                responder.Bind(endpoint);
                requester.Connect(endpoint);
                requester.Disconnect();

                Assert.IsTrue(requester.IsDisconnected);
            }
        }

        [SetUp]
        public void Setup()
        {
            TestHelpers.InitializeMessageSerialization();
        }
    }
}
