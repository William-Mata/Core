using Core.Application.Contracts.Compras;
using Core.Application.DTOs.Compras;
using Core.Application.Services.Compras;
using Core.Domain.Entities.Administracao;
using Core.Domain.Entities.Compras;
using Core.Domain.Entities.Financeiro;
using Core.Domain.Enums;
using Core.Domain.Enums.Compras;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;
using Core.Domain.Interfaces.Compras;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Tests.Unit.Application;

public sealed class ComprasServiceTests
{
    [Fact]
    public async Task DeveCriarListaComParticipanteProprietarioELogInicial()
    {
        var repository = new ComprasRepositoryFake();
        var service = CriarService(repository, new AmizadeRepositoryFake(), 1);

        var resultado = await service.CriarListaAsync(new CriarListaCompraRequest("Supermercado", "Mercado", "Lista da semana"));

        Assert.Equal("Supermercado", resultado.Nome);
        Assert.Single(repository.Listas);
        Assert.Single(repository.Listas[0].Participantes.Where(x => x.UsuarioId == 1 && x.Papel == PapelParticipacaoListaCompra.Proprietario));
        Assert.Single(repository.Listas[0].Logs);
        Assert.Equal(AcaoLogs.Cadastro, repository.Listas[0].Logs[0].Acao);
    }

    [Fact]
    public async Task DeveImpedirCompartilharListaComUsuarioQueNaoEhAmigoAceito()
    {
        var repository = new ComprasRepositoryFake();
        var service = CriarService(repository, new AmizadeRepositoryFake(), 1);
        var lista = await service.CriarListaAsync(new CriarListaCompraRequest("Mercado", "Mercado"));

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CompartilharListaAsync(lista.Id, new CompartilharListaCompraRequest(2, PapelParticipacaoListaCompra.Editor)));

        Assert.Equal("participante_nao_eh_amigo_aceito", ex.Message);
    }

    [Fact]
    public async Task DeveCriarItemComValorTotalCalculadoERegistrarHistoricoDePreco()
    {
        var repository = new ComprasRepositoryFake();
        var amizadeRepository = new AmizadeRepositoryFake();
        var service = CriarService(repository, amizadeRepository, 1);
        var lista = await service.CriarListaAsync(new CriarListaCompraRequest("Feira", "Mercado"));

        var item = await service.CriarItemAsync(lista.Id, new CriarItemListaCompraRequest(
            "Tomate",
            null,
            UnidadeMedidaCompra.Kg,
            2m,
            10m,
            "#ff0000"));

        Assert.Equal(20m, item.ValorTotal);
        Assert.Single(repository.Produtos);
        Assert.Equal(10m, repository.Produtos[0].UltimoPrecoUnitario);
        Assert.Single(repository.Produtos[0].HistoricosPreco);
        Assert.Equal(OrigemPrecoHistoricoCompra.Estimado, repository.Produtos[0].HistoricosPreco[0].Origem);
    }

    [Fact]
    public async Task DeveConverterDesejosEmNovaListaEMarcarComoConvertidos()
    {
        var repository = new ComprasRepositoryFake();
        var service = CriarService(repository, new AmizadeRepositoryFake(), 1);
        await service.CriarDesejoAsync(new CriarDesejoCompraRequest("Arroz", null, UnidadeMedidaCompra.Unidade, 1m, 25m));
        await service.CriarDesejoAsync(new CriarDesejoCompraRequest("Feijao", null, UnidadeMedidaCompra.Unidade, 1m, 10m));

        var desejosIds = repository.Desejos.Select(x => x.Id).ToArray();
        var resultado = await service.ConverterDesejosAsync(new ConverterDesejosCompraRequest(
            desejosIds,
            null,
            "Compras do mes",
            "Mercado",
            AcaoPosConversaoDesejoCompra.MarcarComoConvertido));

        Assert.Equal(2, resultado.ItensCriados);
        var listaCriada = repository.Listas.Single(x => x.Id == resultado.ListaId);
        Assert.Equal(2, listaCriada.Itens.Count);
        Assert.All(repository.Desejos, x => Assert.True(x.Convertido));
    }

    private static ComprasService CriarService(ComprasRepositoryFake repository, AmizadeRepositoryFake amizadeRepository, int? usuarioId) =>
        new(repository, amizadeRepository, new UsuarioRepositoryFake(), new UsuarioAutenticadoProviderFake(usuarioId), new ComprasTempoRealPublisherFake());

    private sealed class UsuarioAutenticadoProviderFake(int? usuarioId) : IUsuarioAutenticadoProvider
    {
        public int? ObterUsuarioId() => usuarioId;
    }

    private sealed class ComprasTempoRealPublisherFake : IComprasTempoRealPublisher
    {
        public Task PublicarAtualizacaoListaAsync(long listaId, string evento, int usuarioId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ComprasRepositoryFake : IComprasRepository
    {
        public List<ListaCompra> Listas { get; } = [];
        public List<Produto> Produtos { get; } = [];
        public List<DesejoCompra> Desejos { get; } = [];

        private long _listaId = 1;
        private long _itemId = 1;
        private long _participacaoId = 1;
        private long _logId = 1;
        private long _produtoId = 1;
        private long _desejoId = 1;
        private long _historicoId = 1;

        public Task<List<ListaCompra>> ListarListasAcessiveisAsync(int usuarioId, bool incluirArquivadas, CancellationToken cancellationToken = default)
        {
            var query = Listas.Where(x =>
                x.UsuarioProprietarioId == usuarioId ||
                x.Participantes.Any(p => p.UsuarioId == usuarioId && p.Status));

            if (!incluirArquivadas)
                query = query.Where(x => x.Status == StatusListaCompra.Ativa);

            return Task.FromResult(query.ToList());
        }

        public Task<ListaCompra?> ObterListaAcessivelPorIdAsync(long listaId, int usuarioId, CancellationToken cancellationToken = default)
        {
            var lista = Listas.FirstOrDefault(x =>
                x.Id == listaId &&
                (x.UsuarioProprietarioId == usuarioId || x.Participantes.Any(p => p.UsuarioId == usuarioId && p.Status)));
            return Task.FromResult(lista);
        }

        public Task<ListaCompra?> ObterListaDoProprietarioAsync(long listaId, int usuarioId, CancellationToken cancellationToken = default)
        {
            var lista = Listas.FirstOrDefault(x => x.Id == listaId && x.UsuarioProprietarioId == usuarioId);
            return Task.FromResult(lista);
        }

        public Task AddListaAsync(ListaCompra lista, CancellationToken cancellationToken = default)
        {
            if (lista.Id == 0)
                lista.Id = _listaId++;

            AtualizarGrafoLista(lista);
            Listas.Add(lista);
            return Task.CompletedTask;
        }

        public Task RemoverListaAsync(ListaCompra lista, CancellationToken cancellationToken = default)
        {
            Listas.Remove(lista);
            return Task.CompletedTask;
        }

        public Task<List<Produto>> BuscarSugestoesProdutosAsync(int usuarioId, string descricao, int limite, CancellationToken cancellationToken = default)
        {
            var sugestoes = Produtos
                .Where(x => x.DescricaoNormalizada.Contains(descricao))
                .Take(limite)
                .ToList();
            return Task.FromResult(sugestoes);
        }

        public Task<Produto?> ObterProdutoPorDescricaoEUnidadeAsync(string descricaoNormalizada, UnidadeMedidaCompra unidade, CancellationToken cancellationToken = default)
        {
            var produto = Produtos.FirstOrDefault(x => x.DescricaoNormalizada == descricaoNormalizada && x.UnidadePadrao == unidade);
            return Task.FromResult(produto);
        }

        public Task AddProdutoAsync(Produto produto, CancellationToken cancellationToken = default)
        {
            if (produto.Id == 0)
                produto.Id = _produtoId++;
            Produtos.Add(produto);
            return Task.CompletedTask;
        }

        public Task<List<DesejoCompra>> ListarDesejosAsync(int usuarioId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Desejos.Where(x => x.UsuarioCadastroId == usuarioId).ToList());

        public Task<DesejoCompra?> ObterDesejoAsync(long desejoId, int usuarioId, CancellationToken cancellationToken = default)
        {
            var desejo = Desejos.FirstOrDefault(x => x.Id == desejoId && x.UsuarioCadastroId == usuarioId);
            return Task.FromResult(desejo);
        }

        public Task<List<DesejoCompra>> ObterDesejosAsync(IReadOnlyCollection<long> desejosIds, int usuarioId, CancellationToken cancellationToken = default)
        {
            var desejos = Desejos.Where(x => x.UsuarioCadastroId == usuarioId && desejosIds.Contains(x.Id)).ToList();
            return Task.FromResult(desejos);
        }

        public Task AddDesejoAsync(DesejoCompra desejo, CancellationToken cancellationToken = default)
        {
            if (desejo.Id == 0)
                desejo.Id = _desejoId++;
            Desejos.Add(desejo);
            return Task.CompletedTask;
        }

        public Task RemoverDesejoAsync(DesejoCompra desejo, CancellationToken cancellationToken = default)
        {
            Desejos.Remove(desejo);
            return Task.CompletedTask;
        }

        public Task<List<HistoricoProduto>> ListarHistoricoPrecosAsync(
            int usuarioId,
            string? descricao,
            UnidadeMedidaCompra? unidade,
            DateTime? dataInicio,
            DateTime? dataFim,
            CancellationToken cancellationToken = default)
        {
            var historicos = Produtos
                .SelectMany(x => x.HistoricosPreco.Select(h =>
                {
                    h.Produto = x;
                    return h;
                }))
                .Where(x => x.UsuarioCadastroId == usuarioId)
                .ToList();

            if (!string.IsNullOrWhiteSpace(descricao))
                historicos = historicos.Where(x => x.Produto?.DescricaoNormalizada.Contains(descricao.Trim().ToLowerInvariant()) == true).ToList();
            if (unidade.HasValue)
                historicos = historicos.Where(x => x.Unidade == unidade.Value).ToList();
            if (dataInicio.HasValue)
                historicos = historicos.Where(x => x.DataHoraCadastro >= dataInicio.Value).ToList();
            if (dataFim.HasValue)
                historicos = historicos.Where(x => x.DataHoraCadastro <= dataFim.Value).ToList();

            return Task.FromResult(historicos);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var lista in Listas)
                AtualizarGrafoLista(lista);

            foreach (var produto in Produtos)
            {
                if (produto.Id == 0)
                    produto.Id = _produtoId++;

                foreach (var historico in produto.HistoricosPreco)
                {
                    if (historico.Id == 0)
                        historico.Id = _historicoId++;
                    historico.ProdutoId = produto.Id;
                    historico.Produto = produto;
                }
            }

            foreach (var desejo in Desejos)
            {
                if (desejo.Id == 0)
                    desejo.Id = _desejoId++;
            }

            return Task.CompletedTask;
        }

        private void AtualizarGrafoLista(ListaCompra lista)
        {
            foreach (var item in lista.Itens)
            {
                if (item.Id == 0)
                    item.Id = _itemId++;
                item.ListaCompraId = lista.Id;
            }

            foreach (var participante in lista.Participantes)
            {
                if (participante.Id == 0)
                    participante.Id = _participacaoId++;
                participante.ListaCompraId = lista.Id;
            }

            foreach (var log in lista.Logs)
            {
                if (log.Id == 0)
                    log.Id = _logId++;
                log.ListaCompraId = lista.Id;
            }
        }
    }

    private sealed class AmizadeRepositoryFake : IAmizadeRepository
    {
        public IReadOnlyCollection<int> AmigosAceitosIds { get; set; } = [];

        public Task<IReadOnlyCollection<Usuario>> ListarAmigosAceitosAsync(int usuarioId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Usuario>>([]);

        public Task<IReadOnlyCollection<int>> ListarIdsAmigosAceitosAsync(int usuarioId, CancellationToken cancellationToken = default) =>
            Task.FromResult(AmigosAceitosIds);

        public Task<IReadOnlyCollection<ConviteAmizade>> ListarConvitesPendentesAsync(int usuarioId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ConviteAmizade>>([]);

        public Task<ConviteAmizade?> ObterConvitePorIdAsync(long conviteId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ConviteAmizade?>(null);

        public Task<ConviteAmizade?> ObterConvitePendenteAsync(int usuarioOrigemId, int usuarioDestinoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ConviteAmizade?>(null);

        public Task<bool> ExisteAmizadeAsync(int usuarioId, int amigoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<Amizade?> ObterAmizadeAsync(int usuarioId, int amigoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Amizade?>(null);

        public Task<ConviteAmizade> CriarConviteAsync(ConviteAmizade convite, CancellationToken cancellationToken = default) =>
            Task.FromResult(convite);

        public Task<ConviteAmizade> AtualizarConviteAsync(ConviteAmizade convite, CancellationToken cancellationToken = default) =>
            Task.FromResult(convite);

        public Task<Amizade> CriarAmizadeAsync(Amizade amizade, CancellationToken cancellationToken = default) =>
            Task.FromResult(amizade);

        public Task ExcluirAmizadeAsync(Amizade amizade, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        public IReadOnlyCollection<Usuario> Usuarios { get; set; } =
        [
            new Usuario { Id = 1, Nome = "William", Email = "william@core.com", Ativo = true },
            new Usuario { Id = 2, Nome = "Alex", Email = "alex@core.com", Ativo = true }
        ];

        public Task<IReadOnlyCollection<Usuario>> ListarAsync(string? filtroId, string? descricao, DateOnly? dataInicio, DateOnly? dataFim, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Usuario>>(Usuarios);

        public Task<IReadOnlyCollection<Usuario>> ListarAtivosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Usuarios.Where(x => x.Ativo).ToArray() as IReadOnlyCollection<Usuario>);

        public Task<Usuario?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Usuarios.FirstOrDefault(x => x.Id == id));

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Usuarios.FirstOrDefault(x => x.Email == email));

        public Task<IReadOnlyCollection<Modulo>> ListarModulosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Modulo>>([]);

        public Task<IReadOnlyCollection<Tela>> ListarTelasAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Tela>>([]);

        public Task<IReadOnlyCollection<Funcionalidade>> ListarFuncionalidadesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Funcionalidade>>([]);

        public Task<bool> ValidarSenhaAsync(Usuario usuario, string senha, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<Usuario> CriarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
            Task.FromResult(usuario);

        public Task<Usuario> AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
            Task.FromResult(usuario);

        public Task SincronizarPermissoesAsync(int usuarioId, int usuarioCadastroId, IReadOnlyCollection<int> modulosAtivosIds, IReadOnlyCollection<int> telasAtivasIds, IReadOnlyCollection<int> funcionalidadesAtivasIds, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AlterarSenhaAsync(Usuario usuario, string novaSenha, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}


