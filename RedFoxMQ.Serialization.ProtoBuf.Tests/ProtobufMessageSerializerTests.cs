﻿// 
// Copyright 2014 Hans Wolff
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
using ProtoBuf;
using System;
using System.IO;

namespace RedFoxMQ.Serialization.ProtoBuf.Tests
{
    [TestFixture]
    public class ProtobufMessageSerializerTests
    {
        [Test]
        public void Serialize_serializes_MessageTypeId()
        {
            var serializer = new ProtobufMessageSerializer();
            var testMessage = new TestMessage();
            var serializedMessage = serializer.Serialize(testMessage);

            using (var mem = new MemoryStream(serializedMessage))
            using (var reader = new BinaryReader(mem))
            {
                var messageTypeId = reader.ReadUInt16();
                Assert.AreEqual(messageTypeId, testMessage.MessageTypeId);
            }
        }

        [Test]
        public void Serialize_Deserialize()
        {
            var serializer = new ProtobufMessageSerializer();
            var testMessage = new TestMessage
            {
                Boolean = true,
                Guid = Guid.NewGuid(),
                Int32 = -1234,
                Int64 = -12345678,
                String = "some text"
            };
            var serializedMessage = serializer.Serialize(testMessage);

            using (var mem = new MemoryStream(serializedMessage))
            {
                mem.Position = 2;
                var deserializedMessage = Serializer.Deserialize<TestMessage>(mem);

                Assert.AreEqual(testMessage.Boolean, deserializedMessage.Boolean);
                Assert.AreEqual(testMessage.Guid, deserializedMessage.Guid);
                Assert.AreEqual(testMessage.Int32, deserializedMessage.Int32);
                Assert.AreEqual(testMessage.Int64, deserializedMessage.Int64);
                Assert.AreEqual(testMessage.String, deserializedMessage.String);
            }
        }
    }
}
