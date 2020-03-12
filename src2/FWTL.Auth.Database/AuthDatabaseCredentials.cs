﻿using FWTL.Common.Credentials;

namespace FWTL.Auth.Database
{
    public class AuthDatabaseCredentials
    {
        public AuthDatabaseCredentials(LocalDatabaseCredentials credentials)
        {
            ConnectionString = credentials.ConnectionString;
        }

        public AuthDatabaseCredentials(SqlServerDatabaseCredentials credentials)
        {
            ConnectionString = credentials.ConnectionString;
        }

        public string ConnectionString { get; }
    }
}