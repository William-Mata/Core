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
    public async Task DeveCriarListaComParticipantesInformadosNoPayload()
    {
        var repository = new ComprasRepositoryFake();
        var amizadeRepository = new AmizadeRepositoryFake { AmigosAceitosIds = [2] };
        var service = CriarService(repository, amizadeRepository, 1);

        var resultado = await service.CriarListaAsync(new CriarListaCompraRequest(
            "Supermercado",
            "Mercado",
            "Lista da semana",
            [
                new ParticipanteListaCompraRequest(1, PapelParticipacaoListaCompra.Proprietario),
                new ParticipanteListaCompraRequest(2, PapelParticipacaoListaCompra.CoProprietario)
            ]));

        Assert.Equal("Supermercado", resultado.Nome);
        Assert.Equal(2, repository.Listas[0].Participantes.Count(x => x.Status));
        Assert.Equal(1, repository.Listas[0].UsuarioProprietarioId);
    }

    [Fact]
    public async Task DeveRetornarPapelUsuarioNaListagemDeListas()
    {
        var repository = new ComprasRepositoryFake();
        repository.Listas.AddRange(
        [
            new ListaCompra
            {
                Id = 1,
                UsuarioProprietarioId = 1,
                Nome = "Lista proprietario",
                Categoria = "Mercado",
                Status = StatusListaCompra.Ativa,
                Participantes =
                [
                    new ParticipacaoListaCompra
                    {
                        UsuarioId = 1,
                        Papel = PapelParticipacaoListaCompra.Proprietario,
                        Status = true
                    }
                ]
            },
            new ListaCompra
            {
                Id = 2,
                UsuarioProprietarioId = 99,
                Nome = "Lista co-proprietario",
                Categoria = "Mercado",
                Status = StatusListaCompra.Ativa,
                Participantes =
                [
                    new ParticipacaoListaCompra
                    {
                        UsuarioId = 1,
                        Papel = PapelParticipacaoListaCompra.CoProprietario,
                        Status = true
                    }
                ]
            },
            new ListaCompra
            {
                Id = 3,
                UsuarioProprietarioId = 98,
                Nome = "Lista leitor",
                Categoria = "Mercado",
                Status = StatusListaCompra.Ativa,
                Participantes =
                [
                    new ParticipacaoListaCompra
                    {
                        UsuarioId = 1,
                        Papel = PapelParticipacaoListaCompra.Leitor,
                        Status = true
                    }
                ]
            }
        ]);

        var service = CriarService(repository, new AmizadeRepositoryFake(), 1);

        var resultado = await service.ListarListasAsync(false);
        var listasPorId = resultado.ToDictionary(x => x.Id);

        Assert.Equal("Proprietario", listasPorId[1].PapelUsuario);
        Assert.Equal("CoProprietario", listasPorId[2].PapelUsuario);
        Assert.Equal("Leitor", listasPorId[3].PapelUsuario);
    }

    [Fact]
    public async Task DeveDuplicarListaComDadosDoBodyEResetarStatusDeCompraDosItens()
    {
        var repository = new ComprasRepositoryFake();
        var service = CriarService(repository, new AmizadeRepositoryFake(), 1);
        var origemDto = await service.CriarListaAsync(new CriarListaCompraRequest("Lista origem", "Mercado", "Obs origem"));
        var origem = repository.Listas.Single(x => x.Id == origemDto.Id);

        origem.Participantes.Add(new ParticipacaoListaCompra
        {
            UsuarioCadastroId = 1,
            UsuarioId = 2,
            Papel = PapelParticipacaoListaCompra.CoProprietario,
            Status = true
        });
        origem.Logs.Add(new ListaCompraLog
        {
            UsuarioCadastroId = 1,
            Acao = AcaoLogs.Atualizacao,
            Descricao = "Log adicional"
        });
        origem.Itens.Add(new ItemListaCompra
        {
            UsuarioCadastroId = 1,
            ProdutoId = 10,
            Descricao = "Arroz",
            DescricaoNormalizada = "arroz",
            Observacao = "Integral",
            Unidade = UnidadeMedidaCompra.Unidade,
            Quantidade = 2,
            PrecoUnitario = 15m,
            ValorTotal = 30m,
            EtiquetaCor = "#00ff00",
            Comprado = true,
            DataHoraCompra = DateTime.UtcNow
        });
        await repository.SaveChangesAsync();

        var duplicadaDto = await service.DuplicarListaAsync(origem.Id, new CriarListaCompraRequest("Lista copia", "Atacado", "Obs nova"));
        var duplicada = repository.Listas.Single(x => x.Id == duplicadaDto.Id);

        Assert.NotEqual(origem.Id, duplicada.Id);
        Assert.Equal("Lista copia", duplicada.Nome);
        Assert.Equal("Atacado", duplicada.Categoria);
        Assert.Equal("Obs nova", duplicada.Observacao);
        Assert.Single(duplicada.Participantes);
        Assert.Single(duplicada.Logs);
        Assert.Single(duplicada.Itens);

        var itemOrigem = origem.Itens.Single();
        var itemDuplicado = duplicada.Itens.Single();
        Assert.NotEqual(itemOrigem.Id, itemDuplicado.Id);
        Assert.Equal(itemOrigem.Descricao, itemDuplicado.Descricao);
        Assert.Equal(itemOrigem.Observacao, itemDuplicado.Observacao);
        Assert.Equal(itemOrigem.Unidade, itemDuplicado.Unidade);
        Assert.Equal(itemOrigem.Quantidade, itemDuplicado.Quantidade);
        Assert.Equal(itemOrigem.PrecoUnitario, itemDuplicado.PrecoUnitario);
        Assert.Equal(itemOrigem.EtiquetaCor, itemDuplicado.EtiquetaCor);
        Assert.False(itemDuplicado.Comprado);
        Assert.Null(itemDuplicado.DataHoraCompra);
    }

    [Fact]
    public async Task DeveValidarCamposObrigatoriosDoBodyAoDuplicarLista()
    {
        var repository = new ComprasRepositoryFake();
        var service = CriarService(repository, new AmizadeRepositoryFake(), 1);
        var origem = await service.CriarListaAsync(new CriarListaCompraRequest("Lista origem", "Mercado"));

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.DuplicarListaAsync(origem.Id, new CriarListaCompraRequest(" ", "Mercado")));

        Assert.Equal("lista_compra_nome_obrigatorio", ex.Message);
    }

    [Fact]
    public async Task DeveImpedirCriacaoComParticipanteQueNaoEhAmigoAceito()
    {
        var repository = new ComprasRepositoryFake();
        var service = CriarService(repository, new AmizadeRepositoryFake(), 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            service.CriarListaAsync(new CriarListaCompraRequest(
                "Mercado",
                "Mercado",
                null,
                [
                    new ParticipanteListaCompraRequest(1, PapelParticipacaoListaCompra.Proprietario),
                    new ParticipanteListaCompraRequest(2, PapelParticipacaoListaCompra.CoProprietario)
                ])));

        Assert.Equal("participante_nao_eh_amigo_aceito", ex.Message);
    }

    [Fact]
    public async Task DeveAtualizarParticipantesDaListaNoEndpointDeEdicao()
    {
        var repository = new ComprasRepositoryFake();
        var amizadeRepository = new AmizadeRepositoryFake { AmigosAceitosIds = [2, 3] };
        var service = CriarService(repository, amizadeRepository, 1);
        var listaCriada = await service.CriarListaAsync(new CriarListaCompraRequest(
            "Mercado",
            "Mercado",
            null,
            [
                new ParticipanteListaCompraRequest(1, PapelParticipacaoListaCompra.Proprietario),
                new ParticipanteListaCompraRequest(2, PapelParticipacaoListaCompra.CoProprietario)
            ]));

        await service.AtualizarListaAsync(listaCriada.Id, new AtualizarListaCompraRequest(
            "Mercado Atualizado",
            "Casa",
            "Obs",
            StatusListaCompra.Ativa,
            [
                new ParticipanteListaCompraRequest(1, PapelParticipacaoListaCompra.Proprietario),
                new ParticipanteListaCompraRequest(3, PapelParticipacaoListaCompra.Leitor)
            ]));

        var lista = repository.Listas.Single(x => x.Id == listaCriada.Id);
        Assert.Equal("Mercado Atualizado", lista.Nome);
        Assert.Equal(2, lista.Participantes.Count(x => x.Status));
        Assert.Contains(lista.Participantes, x => x.UsuarioId == 3 && x.Papel == PapelParticipacaoListaCompra.Leitor && x.Status);
        Assert.Contains(lista.Participantes, x => x.UsuarioId == 2 && !x.Status);
    }

    [Fact]
    public async Task DeveRetornarDetalheSemItensNoEndpointDeMetadados()
    {
        var repository = new ComprasRepositoryFake();
        var amizadeRepository = new AmizadeRepositoryFake { AmigosAceitosIds = [2] };
        var service = CriarService(repository, amizadeRepository, 1);
        var lista = await service.CriarListaAsync(new CriarListaCompraRequest(
            "Mercado",
            "Mercado",
            null,
            [
                new ParticipanteListaCompraRequest(1, PapelParticipacaoListaCompra.Proprietario),
                new ParticipanteListaCompraRequest(2, PapelParticipacaoListaCompra.Leitor)
            ]));

        await service.CriarItemAsync(lista.Id, new CriarItemListaCompraRequest(
            "Tomate",
            null,
            UnidadeMedidaCompra.Kg,
            2m,
            10m,
            null));

        var detalhe = await service.ObterDetalheListaAsync(lista.Id);

        Assert.Equal(lista.Id, detalhe.Id);
        Assert.Equal(2, detalhe.Participantes.Count);
        Assert.NotEmpty(detalhe.Logs);
    }

    [Fact]
    public async Task DeveExporLogsNoEndpointDeObterListaComItens()
    {
        var repository = new ComprasRepositoryFake();
        var service = CriarService(repository, new AmizadeRepositoryFake(), 1);
        var lista = await service.CriarListaAsync(new CriarListaCompraRequest("Mercado", "Mercado"));

        var detalheComItens = await service.ObterListaAsync(lista.Id);

        Assert.Equal(lista.Id, detalheComItens.Id);
        Assert.NotEmpty(detalheComItens.Logs);
    }

    [Fact]
    public async Task DeveRetornarItemCompletoPorId()
    {
        var repository = new ComprasRepositoryFake();
        var service = CriarService(repository, new AmizadeRepositoryFake(), 1);
        var lista = await service.CriarListaAsync(new CriarListaCompraRequest("Feira", "Mercado"));
        var criado = await service.CriarItemAsync(lista.Id, new CriarItemListaCompraRequest("Tomate", "Italiano", UnidadeMedidaCompra.Kg, 2m, 10m, "#ff0000"));

        var item = await service.ObterItemAsync(lista.Id, criado.Id);

        Assert.Equal(criado.Id, item.Id);
        Assert.Equal("Tomate", item.Descricao);
        Assert.Equal(UnidadeMedidaCompra.Kg, item.Unidade);
    }

    [Fact]
    public async Task DeveExcluirItemPorId()
    {
        var repository = new ComprasRepositoryFake();
        var service = CriarService(repository, new AmizadeRepositoryFake(), 1);
        var lista = await service.CriarListaAsync(new CriarListaCompraRequest("Feira", "Mercado"));
        var item1 = await service.CriarItemAsync(lista.Id, new CriarItemListaCompraRequest("Tomate", null, UnidadeMedidaCompra.Kg, 1m, 9m, null));
        await service.CriarItemAsync(lista.Id, new CriarItemListaCompraRequest("Cebola", null, UnidadeMedidaCompra.Kg, 1m, 7m, null));

        await service.ExcluirItemAsync(lista.Id, item1.Id);

        var listaAtualizada = await service.ObterListaAsync(lista.Id);
        Assert.Single(listaAtualizada.Itens);
        Assert.DoesNotContain(listaAtualizada.Itens, x => x.Id == item1.Id);
        Assert.Contains(listaAtualizada.Logs, x => x.Acao == AcaoLogs.Exclusao);
    }

    [Fact]
    public async Task DeveBuscarSugestoesDeItensPorDescricaoParcial()
    {
        var repository = new ComprasRepositoryFake();
        var service = CriarService(repository, new AmizadeRepositoryFake(), 1);
        var lista = await service.CriarListaAsync(new CriarListaCompraRequest("Feira", "Mercado"));
        var criado = await service.CriarItemAsync(lista.Id, new CriarItemListaCompraRequest("Tomate Italiano", "Obs", UnidadeMedidaCompra.Kg, 1m, 9m, null));

        var sugestoes = await service.BuscarSugestoesItensAsync(lista.Id, "tom");

        var sugestao = Assert.Single(sugestoes);
        Assert.Equal(criado.Id, sugestao.Id);
        Assert.Equal("Tomate Italiano", sugestao.Descricao);
        Assert.Equal(UnidadeMedidaCompra.Kg, sugestao.Unidade);
        Assert.Equal(1m, sugestao.Quantidade);
        Assert.Equal(9m, sugestao.PrecoUnitario);
        Assert.Equal(9m, sugestao.ValorTotal);
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

    [Fact]
    public async Task DeveRetornarHistoricoPrecosOrdenadoDeFormaCrescenteComMetricasConsistentes()
    {
        var repository = new ComprasRepositoryFake();
        var service = CriarService(repository, new AmizadeRepositoryFake(), 1);

        repository.Produtos.Add(new Produto
        {
            Id = 101,
            UsuarioCadastroId = 1,
            Descricao = "Arroz",
            DescricaoNormalizada = "arroz",
            UnidadePadrao = UnidadeMedidaCompra.Kg,
            HistoricosPreco =
            [
                new HistoricoProduto { UsuarioCadastroId = 1, ProdutoId = 101, Unidade = UnidadeMedidaCompra.Kg, PrecoUnitario = 32.9m, DataHoraCadastro = new DateTime(2026, 04, 28, 10, 0, 0, DateTimeKind.Utc) },
                new HistoricoProduto { UsuarioCadastroId = 1, ProdutoId = 101, Unidade = UnidadeMedidaCompra.Kg, PrecoUnitario = 27.5m, DataHoraCadastro = new DateTime(2026, 02, 10, 10, 0, 0, DateTimeKind.Utc) },
                new HistoricoProduto { UsuarioCadastroId = 1, ProdutoId = 101, Unidade = UnidadeMedidaCompra.Kg, PrecoUnitario = 29.9m, DataHoraCadastro = new DateTime(2026, 03, 01, 10, 0, 0, DateTimeKind.Utc) }
            ]
        });

        var resultado = await service.ListarHistoricoPrecosAsync(null, null, null, null);
        var produto = Assert.Single(resultado);

        Assert.Equal(32.9m, produto.UltimoPreco);
        Assert.Equal(new DateTime(2026, 04, 28, 10, 0, 0, DateTimeKind.Utc), produto.DataUltimoPreco);
        Assert.Equal(27.5m, produto.MenorPreco);
        Assert.Equal(32.9m, produto.MaiorPreco);
        Assert.Equal(30.1m, produto.MediaPreco);
        Assert.Equal(3, produto.TotalOcorrencias);
        Assert.Collection(produto.HistoricoPrecos,
            item =>
            {
                Assert.Equal(new DateOnly(2026, 02, 10), item.Data);
                Assert.Equal(27.5m, item.Valor);
            },
            item =>
            {
                Assert.Equal(new DateOnly(2026, 03, 01), item.Data);
                Assert.Equal(29.9m, item.Valor);
            },
            item =>
            {
                Assert.Equal(new DateOnly(2026, 04, 28), item.Data);
                Assert.Equal(32.9m, item.Valor);
            });
    }

    [Fact]
    public async Task DeveIgnorarHistoricosSemPrecoValido()
    {
        var repository = new ComprasRepositoryFake();
        var service = CriarService(repository, new AmizadeRepositoryFake(), 1);

        repository.Produtos.Add(new Produto
        {
            Id = 102,
            UsuarioCadastroId = 1,
            Descricao = "Feijao",
            DescricaoNormalizada = "feijao",
            UnidadePadrao = UnidadeMedidaCompra.Kg,
            HistoricosPreco =
            [
                new HistoricoProduto { UsuarioCadastroId = 1, ProdutoId = 102, Unidade = UnidadeMedidaCompra.Kg, PrecoUnitario = 0m, DataHoraCadastro = new DateTime(2026, 02, 10, 10, 0, 0, DateTimeKind.Utc) },
                new HistoricoProduto { UsuarioCadastroId = 1, ProdutoId = 102, Unidade = UnidadeMedidaCompra.Kg, PrecoUnitario = -2m, DataHoraCadastro = new DateTime(2026, 03, 10, 10, 0, 0, DateTimeKind.Utc) }
            ]
        });

        var resultado = await service.ListarHistoricoPrecosAsync(null, null, null, null);

        Assert.Empty(resultado);
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

        public Task<List<ItemListaCompra>> BuscarSugestoesItensAsync(int usuarioId, string descricao, int limite, CancellationToken cancellationToken = default)
        {
            var sugestoes = Listas
                .Where(x => x.UsuarioProprietarioId == usuarioId || x.Participantes.Any(p => p.UsuarioId == usuarioId && p.Status))
                .SelectMany(x => x.Itens)
                .Where(x => x.DescricaoNormalizada.Contains(descricao))
                .OrderBy(x => x.Descricao)
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


