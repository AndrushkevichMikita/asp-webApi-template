﻿namespace ApiTemplate.SharedKernel.ExceptionHandler
{
    public enum ErrorStatus
    {
        InvalidData,
        NotFound,
        Unauthorized,
        Forbidden,
        NotAcceptable,
        PayloadLarge,
    }

    public class MyApplicationException : Exception
    {
        public ErrorStatus ErrorStatus { get; init; }

        public MyApplicationException(ErrorStatus errorStatus) : base()
        {
            ErrorStatus = errorStatus;
        }

        public MyApplicationException(ErrorStatus errorStatus, string message) : base(message)
        {
            ErrorStatus = errorStatus;
        }

        public MyApplicationException(ErrorStatus errorStatus, string message, Exception exception) : base(message, exception)
        {
            ErrorStatus = errorStatus;
        }
    }
}
