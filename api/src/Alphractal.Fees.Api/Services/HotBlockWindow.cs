using System.Numerics;
using Alphractal.Fees.Api.Models.Domain;

namespace Alphractal.Fees.Api.Services;

/// <summary>
/// Janela quente: os ultimos N blocos EM MEMORIA (RN-10). E daqui que o SSE e o
/// snapshot ao vivo saem — nunca do ClickHouse (RN-14).
/// </summary>
/// <remarks>
/// Um produtor (a ingestao) e N leitores (requisicoes HTTP), entao o acesso e
/// protegido por lock. A lista e pequena (300 itens por padrao) e a escrita
/// acontece uma vez a cada ~12 s: contencao aqui e irrelevante, e um lock simples
/// e mais facil de defender que uma estrutura lock-free que ninguem vai revisar.
/// </remarks>
public sealed class HotBlockWindow
{
    private readonly object _gate = new();
    private readonly LinkedList<ChainBlockHeader> _blocks = new();
    private readonly int _capacity;

    public HotBlockWindow(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
    }

    public int Count
    {
        get { lock (_gate) { return _blocks.Count; } }
    }

    public ChainBlockHeader? Latest
    {
        get { lock (_gate) { return _blocks.Last?.Value; } }
    }

    /// <summary>
    /// Insere um bloco. Numero menor ou igual ao ultimo e reorg: substitui, nunca
    /// duplica e nunca mantem os dois em paralelo (RN-08 / RN-16).
    /// </summary>
    /// <returns><c>true</c> quando a insercao foi um reorg.</returns>
    public bool Add(ChainBlockHeader block)
    {
        lock (_gate)
        {
            var reorg = false;

            // Remove do fim tudo que tenha numero >= ao que chegou. Numa cadeia
            // saudavel isso nao remove nada; num reorg descarta o ramo antigo.
            while (_blocks.Last is { } tail && tail.Value.Number >= block.Number)
            {
                _blocks.RemoveLast();
                reorg = true;
            }

            _blocks.AddLast(block);

            while (_blocks.Count > _capacity)
            {
                _blocks.RemoveFirst();
            }

            return reorg;
        }
    }

    public IReadOnlyList<ChainBlockHeader> Snapshot(int max)
    {
        lock (_gate)
        {
            return _blocks.Reverse().Take(max).ToList();
        }
    }

    /// <summary>Maior numero de bloco na janela, ou zero se ela estiver vazia.</summary>
    public BigInteger HighestBlock => Latest?.Number ?? BigInteger.Zero;
}
