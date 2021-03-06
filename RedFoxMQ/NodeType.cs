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

namespace RedFoxMQ
{
    public enum NodeType : byte
    {
        Publisher = 1,
        Subscriber = 2,

        Responder = 3,
        Requester = 4,

        ServiceQueue = 5,
        ServiceQueueReader = 6,
        ServiceQueueWriter = 7,
    }
}
