/*
 * Licensed to the OpenSkywalking under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */

using Grpc.Core;
using SkyWalking.Remote.Authentication;

namespace SkyWalking.Remote
{
    internal class GrpcChannelBuilder
    {
        private string _token;

        private string _server;

        private string _rootCertificatePath;

        public GrpcChannelBuilder WithAuthenticationToken(string token)
        {
            _token = token;
            return this;
        }

        public GrpcChannelBuilder WithServer(string server)
        {
            _server = server;
            return this;
        }

        public GrpcChannelBuilder WithCredential(string rootCertificatePath)
        {
            _rootCertificatePath = rootCertificatePath;
            return this;
        }

        public Channel Build()
        {
            return new Channel(_server, GetCredentials());
        }

        private ChannelCredentials GetCredentials()
        {
            if (_rootCertificatePath != null)
            {
                var authInterceptor = AuthenticationInterceptors.CreateAuthInterceptor(_token);
                return ChannelCredentials.Create(new SslCredentials(), CallCredentials.FromInterceptor(authInterceptor));
            }
            return ChannelCredentials.Insecure;
        }
    }
}
