namespace SecureAuth.Contracts;

public interface ISignedRequest
{
    string ApiSignature { get; }

    long RequestDate { get; }
}
