using System;

namespace Application.Database;

public class DatabaseValidationException : Exception
{
    public DatabaseValidationException(string message) : base(message)
    {
    }
}