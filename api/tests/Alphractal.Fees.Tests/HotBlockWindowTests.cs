using System.Numerics;
using Alphractal.Fees.Api.Models.Domain;
using Alphractal.Fees.Api.Services;
using Xunit;

namespace Alphractal.Fees.Tests;

/// <summary>Janela quente: RN-10 (capacidade) e RN-08/RN-16 (reorg).</summary>
public sealed class HotBlockWindowTests
{
    private static ChainBlockHeader Block(int number, string hash = "0xabc") => new()
    {
        Number = number,
        Hash = hash,
        Timestamp = DateTimeOffset.UnixEpoch.AddSeconds(number * 12),
        BaseFeePerGas = BigInteger.Pow(10, 9),
        GasUsed = 15_000_000,
        GasLimit = 30_000_000,
        ReceivedAt = DateTimeOffset.UnixEpoch.AddSeconds(number * 12 + 1),
    };

    [Fact]
    public void Rn10_janela_nunca_passa_da_capacidade()
    {
        var window = new HotBlockWindow(300);

        foreach (var number in Enumerable.Range(1, 500))
        {
            window.Add(Block(number));
        }

        Assert.Equal(300, window.Count);
        Assert.Equal(new BigInteger(500), window.HighestBlock);
        // Os 200 primeiros sairam da memoria e vivem so no banco.
        Assert.DoesNotContain(window.Snapshot(300), block => block.Number == 200);
    }

    [Fact]
    public void Rn08_bloco_repetido_substitui_e_nao_duplica()
    {
        var window = new HotBlockWindow(10);
        window.Add(Block(1));
        window.Add(Block(2, "0xoriginal"));

        var reorg = window.Add(Block(2, "0xreorg"));

        Assert.True(reorg);
        Assert.Equal(2, window.Count);
        Assert.Equal("0xreorg", window.Latest!.Hash);
    }

    [Fact]
    public void Rn16_reorg_profundo_descarta_o_ramo_antigo_inteiro()
    {
        var window = new HotBlockWindow(10);
        foreach (var number in Enumerable.Range(1, 5))
        {
            window.Add(Block(number, "0xold"));
        }

        // Chega o bloco 3 de outro ramo: 3, 4 e 5 antigos devem sumir.
        var reorg = window.Add(Block(3, "0xnew"));

        Assert.True(reorg);
        Assert.Equal(3, window.Count);
        Assert.Equal(new BigInteger(3), window.HighestBlock);
        Assert.Equal("0xnew", window.Latest!.Hash);
    }

    [Fact]
    public void Sequencia_normal_nao_e_marcada_como_reorg()
    {
        var window = new HotBlockWindow(10);
        window.Add(Block(1));

        Assert.False(window.Add(Block(2)));
    }

    [Fact]
    public void Janela_vazia_nao_tem_ultimo_bloco()
    {
        var window = new HotBlockWindow(10);

        Assert.Null(window.Latest);
        Assert.Equal(BigInteger.Zero, window.HighestBlock);
    }
}
