using System.Threading.Tasks;
using System.Numerics;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using Application.Utils;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Base58Encoder = Application.Utils.Base58Encoder;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Represents CIS token in database
/// </summary>
public class Token
{
    /// <summary>
    /// Token Contract Index
    /// </summary>
    public ulong ContractIndex { get; set; }

    /// <summary>
    /// Token contract Subindex
    /// </summary>
    public ulong ContractSubIndex { get; set; }

    /// <summary>
    /// Token Id
    /// </summary>
    public string TokenId { get; set; }

    /// <summary>
    /// Token Metadata URL
    /// </summary>
    public string? MetadataUrl { get; set; }
    
    /// <summary>
    /// Token address of the token which consist of contract index, contract subindex and token id.
    /// See <see cref="EncodeTokenAddress"/> for calculations.
    /// </summary>
    [GraphQLIgnore]
    public string? TokenAddress { get; set; }

    /// <summary>
    /// Total supply of the token
    /// </summary>
    public BigInteger TotalSupply { get; set; }

    /// <summary>
    /// Get transaction with the initial mint event of the token.
    /// </summary>
    public async Task<Transaction> GetInitialTransaction(GraphQlDbContext context)
    {
        var initialTokenEvent = await context.TokenEvents
            .Where(te => te.ContractIndex == ContractIndex &&
                         te.ContractSubIndex == ContractSubIndex &&
                         te.TokenId == TokenId)
            .OrderBy(t => t.Id)
            .FirstAsync();
        
        return await context.Transactions
            .SingleAsync(t => t.TransactionHash == initialTokenEvent.Event.TransactionHash);
    }

    /// <summary>
    /// Gets accounts with balances for this particular token
    /// </summary>
    /// <param name="dbContext">EF Core Database Context</param>
    /// <returns><see cref="IQueryable<AccountToken>"/></returns>
    [UseOffsetPaging(MaxPageSize = 100, IncludeTotalCount = true)]
    public IQueryable<AccountToken> GetAccounts(GraphQlDbContext dbContext)
    {
        return dbContext.AccountTokens
            .AsNoTracking()
            .Where(t =>
            t.ContractIndex == ContractIndex
            && t.ContractSubIndex == ContractSubIndex
            && t.TokenId == TokenId)
            .OrderByDescending(t => t.AccountId);
    }
        
    [UseOffsetPaging(MaxPageSize = 100, IncludeTotalCount = true)]
    public IQueryable<TokenEvent> GetTokenEvents(GraphQlDbContext dbContext)
    {
        return dbContext.TokenEvents
            .AsNoTracking()
            .Where(t =>
                t.ContractIndex == this.ContractIndex
                && t.ContractSubIndex == this.ContractSubIndex
                && t.TokenId == this.TokenId)
            .OrderByDescending(t => t.Id);
    }
    
    /// <summary>
    /// Encode token address.
    /// It is encoded by using leb128 encoding on contract index
    /// and contract subindex.
    /// The leb128 encodings are concatenated and the token id as bytes are appended.
    /// Finally the whole byte array are base 58 encoded.
    /// </summary>
    internal static string EncodeTokenAddress(
        ulong contractIndex,
        ulong contractSubindex,
        string tokenId
    )
    {
        var contractIndexBytes = Leb128.EncodeUnsignedLeb128(contractIndex);
        var contractSubindexBytes = Leb128.EncodeUnsignedLeb128(contractSubindex);
        var tokenIdBytes = Convert.FromHexString(tokenId).AsSpan();
        Span<byte> bytes = new byte[1 + contractIndexBytes.Length + contractSubindexBytes.Length + tokenIdBytes.Length];
        bytes[0] = 2;
        contractIndexBytes.CopyTo(bytes.Slice(1,
            contractIndexBytes.Length));
        contractSubindexBytes.CopyTo(bytes.Slice(contractIndexBytes.Length + 1,
            contractSubindexBytes.Length));
        tokenIdBytes.CopyTo(bytes.Slice(contractSubindexBytes.Length + contractIndexBytes.Length + 1,
            tokenIdBytes.Length));
            
        return Base58Encoder.Base58CheckEncoder.EncodeData(bytes);
    }

    [ExtendObjectType(typeof(Token))]
    public sealed class TokenExtensions
    {
        public string GetContractAddressFormatted([Parent] Token token) => 
            new ContractAddress(token.ContractIndex, token.ContractSubIndex).AsString;

        public string GetTokenAddress([Parent]Token token) => 
            token.TokenAddress ?? EncodeTokenAddress(token.ContractIndex, token.ContractSubIndex, token.TokenId);
    }
}

[ExtendObjectType(typeof(Query))]
public class TokenQuery
{
    [UsePaging(MaxPageSize = 100)]
    public IQueryable<Token> GetTokens(GraphQlDbContext dbContext) =>
        dbContext.Tokens.OrderByDescending(t => t.ContractIndex).AsNoTracking();

    public Token GetToken(
        GraphQlDbContext dbContext,
        ulong contractIndex,
        ulong contractSubIndex,
        string tokenId) => dbContext.Tokens
        .AsNoTracking()
        .Single(t =>
            t.ContractIndex == contractIndex && t.ContractSubIndex == contractSubIndex && t.TokenId == tokenId);
}
