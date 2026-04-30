using Core.Application.Contracts.Compras;
using Core.Application.DTOs.Compras;
using Core.Domain.Entities.Compras;
using Core.Domain.Enums;
using Core.Domain.Enums.Compras;
using Core.Domain.Exceptions;
using Core.Domain.Interfaces;
using Core.Domain.Interfaces.Administracao;
using Core.Domain.Interfaces.Compras;
using Core.Domain.Interfaces.Financeiro;

namespace Core.Application.Services.Compras;

public sealed class ComprasService(
    IComprasRepository repository,
    IAmizadeRepository amizadeRepository,
    IUsuarioRepository usuarioRepository,
    IUsuarioAutenticadoProvider usuarioAutenticadoProvider,
    IComprasTempoRealPublisher comprasTempoRealPublisher)
{
    public async Task<IReadOnlyCollection<ListaCompraResumoDto>> ListarListasAsync(bool incluirArquivadas, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var listas = await repository.ListarListasAcessiveisAsync(usuarioId, incluirArquivadas, cancellationToken);
        return listas.Select(x => MapResumo(x, usuarioId)).ToArray();
    }

    public async Task<ListaCompraDetalheDto> ObterListaAsync(long listaId, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var lista = await repository.ObterListaAcessivelPorIdAsync(listaId, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
        return await MapDetalheAsync(lista, usuarioId, cancellationToken);
    }

    public async Task<ListaCompraParticipantesDetalheDto> ObterDetalheListaAsync(long listaId, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var lista = await repository.ObterListaAcessivelPorIdAsync(listaId, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
        return await MapDetalheParticipantesAsync(lista, cancellationToken);
    }

    public async Task<ListaCompraDetalheDto> CriarListaAsync(CriarListaCompraRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var nome = NormalizarTextoObrigatorio(request.Nome, "lista_compra_nome_obrigatorio");
        var categoria = NormalizarTextoObrigatorio(request.Categoria, "lista_compra_categoria_obrigatoria");

        var lista = new ListaCompra
        {
            UsuarioCadastroId = usuarioId,
            UsuarioProprietarioId = usuarioId,
            Nome = nome,
            Categoria = categoria,
            Observacao = NormalizarTextoOpcional(request.Observacao),
            Status = StatusListaCompra.Ativa,
            DataHoraAtualizacao = DateTime.UtcNow
        };

        await AtualizarParticipantesAsync(lista, usuarioId, usuarioId, request.Participantes, definirParticipantesPadraoQuandoNulo: true, exigirProprietarioAutenticado: true, cancellationToken);
        lista.Logs.Add(CriarLog(usuarioId, null, AcaoLogs.Cadastro, "Lista criada."));

        await repository.AddListaAsync(lista, cancellationToken);
        await PublicarAtualizacaoListaAsync(lista.Id, "lista_criada", usuarioId, cancellationToken);
        return await MapDetalheAsync(lista, usuarioId, cancellationToken);
    }

    public async Task<ListaCompraDetalheDto> AtualizarListaAsync(long listaId, AtualizarListaCompraRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var lista = await repository.ObterListaAcessivelPorIdAsync(listaId, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
        if (!PodeEditar(lista, usuarioId))
            throw new DomainException("lista_compra_sem_permissao_edicao");

        var nome = NormalizarTextoObrigatorio(request.Nome, "lista_compra_nome_obrigatorio");
        var categoria = NormalizarTextoObrigatorio(request.Categoria, "lista_compra_categoria_obrigatoria");

        var valorAnterior = $"nome={lista.Nome};categoria={lista.Categoria};observacao={lista.Observacao};status={lista.Status}";
        lista.Nome = nome;
        lista.Categoria = categoria;
        lista.Observacao = NormalizarTextoOpcional(request.Observacao);
        if (request.Status.HasValue)
            lista.Status = request.Status.Value;

        await AtualizarParticipantesAsync(lista, usuarioId, lista.UsuarioProprietarioId, request.Participantes, definirParticipantesPadraoQuandoNulo: false, exigirProprietarioAutenticado: false, cancellationToken);

        lista.DataHoraAtualizacao = DateTime.UtcNow;
        lista.Logs.Add(CriarLog(usuarioId, null, AcaoLogs.Atualizacao, "Lista atualizada.", valorAnterior,
            $"nome={lista.Nome};categoria={lista.Categoria};observacao={lista.Observacao};status={lista.Status}"));

        await repository.SaveChangesAsync(cancellationToken);
        await PublicarAtualizacaoListaAsync(lista.Id, "lista_atualizada", usuarioId, cancellationToken);
        return await MapDetalheAsync(lista, usuarioId, cancellationToken);
    }

    public async Task<ListaCompraDetalheDto> ArquivarListaAsync(long listaId, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var lista = await repository.ObterListaDoProprietarioAsync(listaId, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
        if (lista.Status == StatusListaCompra.Arquivada)
            throw new DomainException("lista_compra_ja_arquivada");

        lista.Status = StatusListaCompra.Arquivada;
        lista.DataHoraAtualizacao = DateTime.UtcNow;
        lista.Logs.Add(CriarLog(usuarioId, null, AcaoLogs.Atualizacao, "Lista arquivada."));
        await repository.SaveChangesAsync(cancellationToken);
        await PublicarAtualizacaoListaAsync(lista.Id, "lista_arquivada", usuarioId, cancellationToken);
        return await MapDetalheAsync(lista, usuarioId, cancellationToken);
    }

    public async Task ExcluirListaAsync(long listaId, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var lista = await repository.ObterListaDoProprietarioAsync(listaId, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
        await repository.RemoverListaAsync(lista, cancellationToken);
        await PublicarAtualizacaoListaAsync(lista.Id, "lista_excluida", usuarioId, cancellationToken);
    }

    public async Task<ListaCompraDetalheDto> DuplicarListaAsync(long listaId, CriarListaCompraRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var lista = await repository.ObterListaAcessivelPorIdAsync(listaId, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
        if (!PodeEditar(lista, usuarioId))
            throw new DomainException("lista_compra_sem_permissao_edicao");
        var nome = NormalizarTextoObrigatorio(request.Nome, "lista_compra_nome_obrigatorio");
        var categoria = NormalizarTextoObrigatorio(request.Categoria, "lista_compra_categoria_obrigatoria");

        var novaLista = new ListaCompra
        {
            UsuarioCadastroId = usuarioId,
            UsuarioProprietarioId = usuarioId,
            Nome = nome,
            Categoria = categoria,
            Observacao = NormalizarTextoOpcional(request.Observacao),
            Status = StatusListaCompra.Ativa,
            DataHoraAtualizacao = DateTime.UtcNow
        };
        novaLista.Participantes.Add(new ParticipacaoListaCompra
        {
            UsuarioCadastroId = usuarioId,
            UsuarioId = usuarioId,
            Papel = PapelParticipacaoListaCompra.Proprietario,
            Status = true
        });

        foreach (var item in lista.Itens)
        {
            var novoItem = new ItemListaCompra
            {
                UsuarioCadastroId = usuarioId,
                ProdutoId = item.ProdutoId,
                Descricao = item.Descricao,
                DescricaoNormalizada = item.DescricaoNormalizada,
                Observacao = item.Observacao,
                Unidade = item.Unidade,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario,
                EtiquetaCor = item.EtiquetaCor,
                Comprado = false,
                DataHoraCompra = null
            };
            AtualizarValorTotalItem(novoItem);
            novaLista.Itens.Add(novoItem);
        }

        novaLista.Logs.Add(CriarLog(usuarioId, null, AcaoLogs.Cadastro, $"Lista duplicada a partir da lista {lista.Id}."));
        await repository.AddListaAsync(novaLista, cancellationToken);
        await PublicarAtualizacaoListaAsync(novaLista.Id, "lista_duplicada", usuarioId, cancellationToken);
        return await MapDetalheAsync(novaLista, usuarioId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ItemListaCompraDto>> BuscarSugestoesItensAsync(long listaId, string? descricao, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        if (descricao?.Trim().Length < 3)
            return [];

        var lista = await repository.ObterListaAcessivelPorIdAsync(listaId, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
        if (!PodeVisualizar(lista, usuarioId))
            throw new DomainException("lista_compra_sem_permissao_visualizacao");

        var sugestoes = await repository.BuscarSugestoesItensAsync(usuarioId, NormalizarDescricao(descricao!), 10, cancellationToken);
        return sugestoes.Select(MapItem).ToArray();
    }

    public async Task<ItemListaCompraDto> ObterItemAsync(long listaId, long itemId, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var lista = await repository.ObterListaAcessivelPorIdAsync(listaId, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
        if (!PodeVisualizar(lista, usuarioId))
            throw new DomainException("lista_compra_sem_permissao_visualizacao");

        var item = lista.Itens.FirstOrDefault(x => x.Id == itemId) ?? throw new NotFoundException("item_lista_compra_nao_encontrado");
        return MapItem(item);
    }

    public async Task<ItemListaCompraDto> CriarItemAsync(long listaId, CriarItemListaCompraRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var lista = await repository.ObterListaAcessivelPorIdAsync(listaId, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
        if (!PodeEditar(lista, usuarioId))
            throw new DomainException("lista_compra_sem_permissao_edicao");

        ValidarQuantidade(request.Quantidade);
        var descricao = NormalizarTextoObrigatorio(request.Descricao, "item_lista_compra_descricao_obrigatoria");
        var descricaoNormalizada = NormalizarDescricao(descricao);
        var produto = await ObterOuCriarProdutoAsync(usuarioId, descricao, descricaoNormalizada, request.Unidade, request.Observacao, request.PrecoUnitario, cancellationToken);

        var item = new ItemListaCompra
        {
            UsuarioCadastroId = usuarioId,
            ProdutoId = produto.Id == 0 ? null : produto.Id,
            Produto = produto,
            Descricao = descricao,
            DescricaoNormalizada = descricaoNormalizada,
            Observacao = NormalizarTextoOpcional(request.Observacao),
            Unidade = request.Unidade,
            Quantidade = request.Quantidade,
            PrecoUnitario = request.PrecoUnitario,
            EtiquetaCor = NormalizarTextoOpcional(request.EtiquetaCor),
            Comprado = false,
            DataHoraCompra = null
        };
        AtualizarValorTotalItem(item);

        lista.Itens.Add(item);
        lista.DataHoraAtualizacao = DateTime.UtcNow;
        lista.Logs.Add(CriarLog(usuarioId, null, AcaoLogs.Cadastro, $"Item '{item.Descricao}' adicionado."));

        if (item.PrecoUnitario.HasValue && item.PrecoUnitario.Value > 0)
        {
            produto.UltimoPrecoUnitario = item.PrecoUnitario;
            produto.DataHoraUltimoPreco = DateTime.UtcNow;
            produto.HistoricosPreco.Add(new HistoricoProduto
            {
                UsuarioCadastroId = usuarioId,
                ItemListaCompra = item,
                Unidade = item.Unidade,
                PrecoUnitario = item.PrecoUnitario.Value,
                Origem = OrigemPrecoHistoricoCompra.Estimado
            });
        }

        await repository.SaveChangesAsync(cancellationToken);
        await PublicarAtualizacaoListaAsync(lista.Id, "item_criado", usuarioId, cancellationToken);
        return MapItem(item);
    }

    public async Task<ItemListaCompraDto> AtualizarItemAsync(long listaId, long itemId, AtualizarItemListaCompraRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var lista = await repository.ObterListaAcessivelPorIdAsync(listaId, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
        if (!PodeEditar(lista, usuarioId))
            throw new DomainException("lista_compra_sem_permissao_edicao");

        var item = lista.Itens.FirstOrDefault(x => x.Id == itemId) ?? throw new NotFoundException("item_lista_compra_nao_encontrado");
        ValidarQuantidade(request.Quantidade);
        var descricao = NormalizarTextoObrigatorio(request.Descricao, "item_lista_compra_descricao_obrigatoria");
        var descricaoNormalizada = NormalizarDescricao(descricao);
        var produto = await ObterOuCriarProdutoAsync(usuarioId, descricao, descricaoNormalizada, request.Unidade, request.Observacao, request.PrecoUnitario, cancellationToken);
        var valorAnterior = $"quantidade={item.Quantidade};preco={item.PrecoUnitario};comprado={item.Comprado}";

        item.UsuarioCadastroId = usuarioId;
        item.ProdutoId = produto.Id == 0 ? null : produto.Id;
        item.Produto = produto;
        item.Descricao = descricao;
        item.DescricaoNormalizada = descricaoNormalizada;
        item.Observacao = NormalizarTextoOpcional(request.Observacao);
        item.Unidade = request.Unidade;
        item.Quantidade = request.Quantidade;
        item.PrecoUnitario = request.PrecoUnitario;
        item.EtiquetaCor = NormalizarTextoOpcional(request.EtiquetaCor);
        item.Comprado = request.Comprado;
        item.DataHoraCompra = request.Comprado ? DateTime.UtcNow : null;
        AtualizarValorTotalItem(item);

        lista.DataHoraAtualizacao = DateTime.UtcNow;
        lista.Logs.Add(CriarLog(usuarioId, item.Id, AcaoLogs.Atualizacao, $"Item '{item.Descricao}' atualizado.", valorAnterior,
            $"quantidade={item.Quantidade};preco={item.PrecoUnitario};comprado={item.Comprado}"));

        RegistrarHistoricoSePossuirPreco(usuarioId, item, produto, item.Comprado ? OrigemPrecoHistoricoCompra.Confirmado : OrigemPrecoHistoricoCompra.Estimado);
        await repository.SaveChangesAsync(cancellationToken);
        await PublicarAtualizacaoListaAsync(lista.Id, "item_atualizado", usuarioId, cancellationToken);
        return MapItem(item);
    }

    public async Task<ItemListaCompraDto> AtualizarItemEdicaoRapidaAsync(long listaId, long itemId, EdicaoRapidaItemListaCompraRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var lista = await repository.ObterListaAcessivelPorIdAsync(listaId, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
        if (!PodeEditar(lista, usuarioId))
            throw new DomainException("lista_compra_sem_permissao_edicao");

        var item = lista.Itens.FirstOrDefault(x => x.Id == itemId) ?? throw new NotFoundException("item_lista_compra_nao_encontrado");
        ValidarQuantidade(request.Quantidade);
        var valorAnterior = $"quantidade={item.Quantidade};preco={item.PrecoUnitario}";

        item.UsuarioCadastroId = usuarioId;
        item.Quantidade = request.Quantidade;
        item.PrecoUnitario = request.PrecoUnitario;
        AtualizarValorTotalItem(item);
        lista.DataHoraAtualizacao = DateTime.UtcNow;
        lista.Logs.Add(CriarLog(usuarioId, item.Id, AcaoLogs.Atualizacao, $"Edicao rapida no item '{item.Descricao}'.", valorAnterior,
            $"quantidade={item.Quantidade};preco={item.PrecoUnitario}"));

        if (item.Produto is not null)
            RegistrarHistoricoSePossuirPreco(usuarioId, item, item.Produto, item.Comprado ? OrigemPrecoHistoricoCompra.Confirmado : OrigemPrecoHistoricoCompra.Estimado);

        await repository.SaveChangesAsync(cancellationToken);
        await PublicarAtualizacaoListaAsync(lista.Id, "item_edicao_rapida", usuarioId, cancellationToken);
        return MapItem(item);
    }

    public async Task<ItemListaCompraDto> MarcarItemCompradoAsync(long listaId, long itemId, MarcarCompradoItemListaCompraRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var lista = await repository.ObterListaAcessivelPorIdAsync(listaId, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
        if (!PodeEditar(lista, usuarioId))
            throw new DomainException("lista_compra_sem_permissao_edicao");

        var item = lista.Itens.FirstOrDefault(x => x.Id == itemId) ?? throw new NotFoundException("item_lista_compra_nao_encontrado");
        item.UsuarioCadastroId = usuarioId;
        item.Comprado = request.Comprado;
        item.DataHoraCompra = request.Comprado ? DateTime.UtcNow : null;
        lista.DataHoraAtualizacao = DateTime.UtcNow;
        lista.Logs.Add(CriarLog(usuarioId, item.Id, AcaoLogs.Atualizacao, $"Item '{item.Descricao}' {(request.Comprado ? "marcado" : "desmarcado")} como comprado."));

        if (request.Comprado && item.Produto is not null)
            RegistrarHistoricoSePossuirPreco(usuarioId, item, item.Produto, OrigemPrecoHistoricoCompra.Confirmado);

        await repository.SaveChangesAsync(cancellationToken);
        await PublicarAtualizacaoListaAsync(lista.Id, request.Comprado ? "item_comprado" : "item_desmarcado", usuarioId, cancellationToken);
        return MapItem(item);
    }

    public async Task ExcluirItemAsync(long listaId, long itemId, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var lista = await repository.ObterListaAcessivelPorIdAsync(listaId, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
        if (!PodeEditar(lista, usuarioId))
            throw new DomainException("lista_compra_sem_permissao_edicao");

        var item = lista.Itens.FirstOrDefault(x => x.Id == itemId) ?? throw new NotFoundException("item_lista_compra_nao_encontrado");
        DesvincularLogsDosItens(lista, [item.Id]);
        lista.Itens.Remove(item);
        lista.DataHoraAtualizacao = DateTime.UtcNow;
        lista.Logs.Add(CriarLog(usuarioId, null, AcaoLogs.Exclusao, $"Item '{item.Descricao}' removido."));

        await repository.SaveChangesAsync(cancellationToken);
        await PublicarAtualizacaoListaAsync(lista.Id, "item_excluido", usuarioId, cancellationToken);
    }

    public async Task<AcaoLoteListaCompraResultadoDto> ExecutarAcaoLoteAsync(long listaId, AcaoLoteListaCompraRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var lista = await repository.ObterListaAcessivelPorIdAsync(listaId, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
        if (!PodeEditar(lista, usuarioId))
            throw new DomainException("lista_compra_sem_permissao_edicao");

        var itensSelecionados = (request.ItensIds ?? [])
            .Distinct()
            .Select(id => lista.Itens.FirstOrDefault(x => x.Id == id))
            .Where(x => x is not null)
            .Cast<ItemListaCompra>()
            .ToList();
        long? novaListaId = null;
        var itensAfetados = 0;

        switch (request.Acao)
        {
            case TipoAcaoLoteListaCompra.MarcarSelecionadosComoComprados:
                foreach (var item in itensSelecionados)
                {
                    item.Comprado = true;
                    item.DataHoraCompra = DateTime.UtcNow;
                    item.UsuarioCadastroId = usuarioId;
                    if (item.Produto is not null)
                        RegistrarHistoricoSePossuirPreco(usuarioId, item, item.Produto, OrigemPrecoHistoricoCompra.Confirmado);
                }
                itensAfetados = itensSelecionados.Count;
                break;
            case TipoAcaoLoteListaCompra.DesmarcarSelecionados:
                foreach (var item in itensSelecionados)
                {
                    item.Comprado = false;
                    item.DataHoraCompra = null;
                    item.UsuarioCadastroId = usuarioId;
                }
                itensAfetados = itensSelecionados.Count;
                break;
            case TipoAcaoLoteListaCompra.ExcluirSelecionados:
                DesvincularLogsDosItens(lista, itensSelecionados.Select(x => x.Id).ToArray());
                foreach (var item in itensSelecionados)
                    lista.Itens.Remove(item);
                itensAfetados = itensSelecionados.Count;
                break;
            case TipoAcaoLoteListaCompra.ExcluirComprados:
                var itensComprados = lista.Itens.Where(x => x.Comprado).ToArray();
                DesvincularLogsDosItens(lista, itensComprados.Select(x => x.Id).ToArray());
                foreach (var item in itensComprados)
                    lista.Itens.Remove(item);
                itensAfetados = itensComprados.Length;
                break;
            case TipoAcaoLoteListaCompra.ExcluirNaoComprados:
                var itensNaoComprados = lista.Itens.Where(x => !x.Comprado).ToArray();
                DesvincularLogsDosItens(lista, itensNaoComprados.Select(x => x.Id).ToArray());
                foreach (var item in itensNaoComprados)
                    lista.Itens.Remove(item);
                itensAfetados = itensNaoComprados.Length;
                break;
            case TipoAcaoLoteListaCompra.ExcluirSemPreco:
                var itensSemPreco = lista.Itens.Where(x => !x.PrecoUnitario.HasValue || x.PrecoUnitario.Value <= 0).ToArray();
                DesvincularLogsDosItens(lista, itensSemPreco.Select(x => x.Id).ToArray());
                foreach (var item in itensSemPreco)
                    lista.Itens.Remove(item);
                itensAfetados = itensSemPreco.Length;
                break;
            case TipoAcaoLoteListaCompra.LimparLista:
                var idsTodosItens = lista.Itens.Select(x => x.Id).ToArray();
                DesvincularLogsDosItens(lista, idsTodosItens);
                itensAfetados = idsTodosItens.Length;
                lista.Itens.Clear();
                break;
            case TipoAcaoLoteListaCompra.ResetarPrecos:
                foreach (var item in lista.Itens)
                {
                    item.PrecoUnitario = null;
                    item.ValorTotal = 0m;
                    item.UsuarioCadastroId = usuarioId;
                }
                itensAfetados = lista.Itens.Count;
                break;
            case TipoAcaoLoteListaCompra.ResetarCores:
                foreach (var item in lista.Itens)
                {
                    item.EtiquetaCor = null;
                    item.UsuarioCadastroId = usuarioId;
                }
                itensAfetados = lista.Itens.Count;
                break;
            case TipoAcaoLoteListaCompra.CriarNovaListaComComprados:
                novaListaId = await CriarNovaListaPorFiltroAsync(usuarioId, lista, request.NomeNovaLista, request.CategoriaNovaLista, x => x.Comprado, cancellationToken);
                itensAfetados = lista.Itens.Count(x => x.Comprado);
                break;
            case TipoAcaoLoteListaCompra.CriarNovaListaComNaoComprados:
                novaListaId = await CriarNovaListaPorFiltroAsync(usuarioId, lista, request.NomeNovaLista, request.CategoriaNovaLista, x => !x.Comprado, cancellationToken);
                itensAfetados = lista.Itens.Count(x => !x.Comprado);
                break;
            case TipoAcaoLoteListaCompra.DuplicarLista:
                var duplicada = await DuplicarListaAsync(
                    listaId,
                    new CriarListaCompraRequest(
                        string.IsNullOrWhiteSpace(request.NomeNovaLista) ? $"{lista.Nome} (copia)" : request.NomeNovaLista.Trim(),
                        string.IsNullOrWhiteSpace(request.CategoriaNovaLista) ? lista.Categoria : request.CategoriaNovaLista.Trim(),
                        lista.Observacao),
                    cancellationToken);
                novaListaId = duplicada.Id;
                itensAfetados = lista.Itens.Count;
                break;
            case TipoAcaoLoteListaCompra.MesclarDuplicados:
                itensAfetados = MesclarDuplicados(lista, usuarioId);
                break;
            default:
                throw new DomainException("acao_lote_invalida");
        }

        lista.DataHoraAtualizacao = DateTime.UtcNow;
        lista.Logs.Add(CriarLog(usuarioId, null, AcaoLogs.Atualizacao, $"Acao em lote executada: {request.Acao}.", null, $"itens_afetados={itensAfetados}"));
        await repository.SaveChangesAsync(cancellationToken);
        await PublicarAtualizacaoListaAsync(lista.Id, "lote_executado", usuarioId, cancellationToken);

        return new AcaoLoteListaCompraResultadoDto(request.Acao.ToString(), itensAfetados, novaListaId);
    }

    public async Task<IReadOnlyCollection<DesejoCompraDto>> ListarDesejosAsync(CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var desejos = await repository.ListarDesejosAsync(usuarioId, cancellationToken);
        return desejos.Select(MapDesejo).ToArray();
    }

    public async Task<DesejoCompraDto> CriarDesejoAsync(CriarDesejoCompraRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        ValidarQuantidade(request.Quantidade);
        var descricao = NormalizarTextoObrigatorio(request.Descricao, "desejo_compra_descricao_obrigatoria");
        var descricaoNormalizada = NormalizarDescricao(descricao);

        var produto = await ObterOuCriarProdutoAsync(usuarioId, descricao, descricaoNormalizada, request.Unidade, request.Observacao, request.PrecoEstimado, cancellationToken);
        var desejo = new DesejoCompra
        {
            UsuarioCadastroId = usuarioId,
            ProdutoId = produto.Id == 0 ? null : produto.Id,
            Produto = produto,
            Descricao = descricao,
            DescricaoNormalizada = descricaoNormalizada,
            Observacao = NormalizarTextoOpcional(request.Observacao),
            Unidade = request.Unidade,
            Quantidade = request.Quantidade,
            PrecoEstimado = request.PrecoEstimado,
            Convertido = false
        };

        await repository.AddDesejoAsync(desejo, cancellationToken);
        return MapDesejo(desejo);
    }

    public async Task<DesejoCompraDto> AtualizarDesejoAsync(long desejoId, AtualizarDesejoCompraRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var desejo = await repository.ObterDesejoAsync(desejoId, usuarioId, cancellationToken) ?? throw new NotFoundException("desejo_compra_nao_encontrado");
        ValidarQuantidade(request.Quantidade);
        var descricao = NormalizarTextoObrigatorio(request.Descricao, "desejo_compra_descricao_obrigatoria");
        var descricaoNormalizada = NormalizarDescricao(descricao);
        var produto = await ObterOuCriarProdutoAsync(usuarioId, descricao, descricaoNormalizada, request.Unidade, request.Observacao, request.PrecoEstimado, cancellationToken);

        desejo.UsuarioCadastroId = usuarioId;
        desejo.ProdutoId = produto.Id == 0 ? null : produto.Id;
        desejo.Produto = produto;
        desejo.Descricao = descricao;
        desejo.DescricaoNormalizada = descricaoNormalizada;
        desejo.Observacao = NormalizarTextoOpcional(request.Observacao);
        desejo.Unidade = request.Unidade;
        desejo.Quantidade = request.Quantidade;
        desejo.PrecoEstimado = request.PrecoEstimado;

        await repository.SaveChangesAsync(cancellationToken);
        return MapDesejo(desejo);
    }

    public async Task ExcluirDesejoAsync(long desejoId, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var desejo = await repository.ObterDesejoAsync(desejoId, usuarioId, cancellationToken) ?? throw new NotFoundException("desejo_compra_nao_encontrado");
        await repository.RemoverDesejoAsync(desejo, cancellationToken);
    }

    public async Task<ResultadoConversaoDesejosDto> ConverterDesejosAsync(ConverterDesejosCompraRequest request, CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        if (request.DesejosIds.Count == 0)
            throw new DomainException("desejos_nao_informados");

        var desejos = await repository.ObterDesejosAsync(request.DesejosIds.Distinct().ToArray(), usuarioId, cancellationToken);
        if (desejos.Count == 0)
            throw new NotFoundException("desejos_nao_encontrados");

        ListaCompra listaDestino;
        if (request.ListaDestinoId.HasValue)
        {
            listaDestino = await repository.ObterListaAcessivelPorIdAsync(request.ListaDestinoId.Value, usuarioId, cancellationToken) ?? throw new NotFoundException("lista_compra_nao_encontrada");
            if (!PodeEditar(listaDestino, usuarioId))
                throw new DomainException("lista_compra_sem_permissao_edicao");
        }
        else
        {
            var nome = string.IsNullOrWhiteSpace(request.NomeNovaLista) ? "Lista criada por desejos" : request.NomeNovaLista.Trim();
            var categoria = string.IsNullOrWhiteSpace(request.CategoriaNovaLista) ? "geral" : request.CategoriaNovaLista.Trim();
            listaDestino = new ListaCompra
            {
                UsuarioCadastroId = usuarioId,
                UsuarioProprietarioId = usuarioId,
                Nome = nome,
                Categoria = categoria,
                Status = StatusListaCompra.Ativa,
                DataHoraAtualizacao = DateTime.UtcNow
            };
            listaDestino.Participantes.Add(new ParticipacaoListaCompra
            {
                UsuarioCadastroId = usuarioId,
                UsuarioId = usuarioId,
                Papel = PapelParticipacaoListaCompra.Proprietario,
                Status = true
            });
            listaDestino.Logs.Add(CriarLog(usuarioId, null, AcaoLogs.Cadastro, "Lista criada a partir da conversao de desejos."));
            await repository.AddListaAsync(listaDestino, cancellationToken);
        }

        foreach (var desejo in desejos)
        {
            var item = new ItemListaCompra
            {
                UsuarioCadastroId = usuarioId,
                ProdutoId = desejo.ProdutoId,
                Descricao = desejo.Descricao,
                DescricaoNormalizada = desejo.DescricaoNormalizada,
                Observacao = desejo.Observacao,
                Unidade = desejo.Unidade,
                Quantidade = desejo.Quantidade,
                PrecoUnitario = desejo.PrecoEstimado,
                ValorTotal = (desejo.PrecoEstimado ?? 0m) * desejo.Quantidade,
                Comprado = false
            };
            listaDestino.Itens.Add(item);

            if (request.AcaoPosConversao == AcaoPosConversaoDesejoCompra.MarcarComoConvertido)
            {
                desejo.Convertido = true;
                desejo.DataHoraConversao = DateTime.UtcNow;
                desejo.UsuarioCadastroId = usuarioId;
            }

            if (request.AcaoPosConversao == AcaoPosConversaoDesejoCompra.Arquivar)
                await repository.RemoverDesejoAsync(desejo, cancellationToken);
        }

        listaDestino.DataHoraAtualizacao = DateTime.UtcNow;
        listaDestino.Logs.Add(CriarLog(usuarioId, null, AcaoLogs.Atualizacao, $"{desejos.Count} desejo(s) convertido(s) em item(ns)."));
        await repository.SaveChangesAsync(cancellationToken);
        await PublicarAtualizacaoListaAsync(listaDestino.Id, "desejos_convertidos", usuarioId, cancellationToken);

        return new ResultadoConversaoDesejosDto(listaDestino.Id, desejos.Count, desejos.Count);
    }

    public async Task<IReadOnlyCollection<HistoricoProdutoDto>> ListarHistoricoPrecosAsync(
        string? descricao,
        UnidadeMedidaCompra? unidade,
        DateTime? dataInicio,
        DateTime? dataFim,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioAutenticadoId();
        var historicos = await repository.ListarHistoricoPrecosAsync(usuarioId, NormalizarTextoOpcional(descricao), unidade, dataInicio, dataFim, cancellationToken);

        return historicos
            .GroupBy(x => new
            {
                x.ProdutoId,
                x.Unidade,
                Descricao = x.Produto?.Descricao ?? string.Empty
            })
            .Select(grupo =>
            {
                var historicoValidoOrdenado = grupo
                    .Where(x => x.PrecoUnitario > 0)
                    .OrderBy(x => x.DataHoraCadastro)
                    .ToArray();

                if (historicoValidoOrdenado.Length == 0)
                    return null;

                var ultimoHistorico = historicoValidoOrdenado[^1];
                var valores = historicoValidoOrdenado.Select(x => x.PrecoUnitario).ToArray();
                return new HistoricoProdutoDto(
                    grupo.Key.ProdutoId,
                    grupo.Key.Descricao,
                    grupo.Key.Unidade,
                    ultimoHistorico.PrecoUnitario,
                    valores.Min(),
                    valores.Max(),
                    decimal.Round(valores.Average(), 2),
                    ultimoHistorico.DataHoraCadastro,
                    valores.Length,
                    historicoValidoOrdenado
                        .Select(x => new HistoricoPrecoItemDto(
                            DateOnly.FromDateTime(x.DataHoraCadastro),
                            x.PrecoUnitario))
                        .ToArray());
            })
            .Where(x => x is not null)
            .Select(x => x!)
            .OrderBy(x => x.Descricao)
            .ToArray();
    }

    private async Task<Produto> ObterOuCriarProdutoAsync(
        int usuarioId,
        string descricao,
        string descricaoNormalizada,
        UnidadeMedidaCompra unidade,
        string? observacao,
        decimal? precoUnitario,
        CancellationToken cancellationToken)
    {
        var produto = await repository.ObterProdutoPorDescricaoEUnidadeAsync(descricaoNormalizada, unidade, cancellationToken);
        if (produto is null)
        {
            produto = new Produto
            {
                UsuarioCadastroId = usuarioId,
                Descricao = descricao,
                DescricaoNormalizada = descricaoNormalizada,
                UnidadePadrao = unidade,
                ObservacaoPadrao = NormalizarTextoOpcional(observacao),
                UltimoPrecoUnitario = precoUnitario > 0 ? precoUnitario : null,
                DataHoraUltimoPreco = precoUnitario > 0 ? DateTime.UtcNow : null
            };
            await repository.AddProdutoAsync(produto, cancellationToken);
        }
        else
        {
            produto.Descricao = descricao;
            produto.UnidadePadrao = unidade;
            produto.ObservacaoPadrao = NormalizarTextoOpcional(observacao);
            if (precoUnitario > 0)
            {
                produto.UltimoPrecoUnitario = precoUnitario;
                produto.DataHoraUltimoPreco = DateTime.UtcNow;
            }
        }

        return produto;
    }

    private async Task<long> CriarNovaListaPorFiltroAsync(
        int usuarioId,
        ListaCompra listaOrigem,
        string? nomeNovaLista,
        string? categoriaNovaLista,
        Func<ItemListaCompra, bool> filtro,
        CancellationToken cancellationToken)
    {
        var nome = string.IsNullOrWhiteSpace(nomeNovaLista) ? $"{listaOrigem.Nome} (derivada)" : nomeNovaLista.Trim();
        var categoria = string.IsNullOrWhiteSpace(categoriaNovaLista) ? listaOrigem.Categoria : categoriaNovaLista.Trim();
        var novaLista = new ListaCompra
        {
            UsuarioCadastroId = usuarioId,
            UsuarioProprietarioId = usuarioId,
            Nome = nome,
            Categoria = categoria,
            Observacao = listaOrigem.Observacao,
            Status = StatusListaCompra.Ativa,
            DataHoraAtualizacao = DateTime.UtcNow
        };
        novaLista.Participantes.Add(new ParticipacaoListaCompra
        {
            UsuarioCadastroId = usuarioId,
            UsuarioId = usuarioId,
            Papel = PapelParticipacaoListaCompra.Proprietario,
            Status = true
        });

        foreach (var item in listaOrigem.Itens.Where(filtro))
        {
            novaLista.Itens.Add(new ItemListaCompra
            {
                UsuarioCadastroId = usuarioId,
                ProdutoId = item.ProdutoId,
                Descricao = item.Descricao,
                DescricaoNormalizada = item.DescricaoNormalizada,
                Observacao = item.Observacao,
                Unidade = item.Unidade,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario,
                ValorTotal = item.ValorTotal,
                EtiquetaCor = item.EtiquetaCor,
                Comprado = item.Comprado,
                DataHoraCompra = item.DataHoraCompra
            });
        }

        novaLista.Logs.Add(CriarLog(usuarioId, null, AcaoLogs.Cadastro, $"Lista criada a partir da lista {listaOrigem.Id}."));
        await repository.AddListaAsync(novaLista, cancellationToken);
        await PublicarAtualizacaoListaAsync(novaLista.Id, "lista_derivada_criada", usuarioId, cancellationToken);
        return novaLista.Id;
    }

    private Task PublicarAtualizacaoListaAsync(
        long listaId,
        string evento,
        int usuarioId,
        CancellationToken cancellationToken) =>
        comprasTempoRealPublisher.PublicarAtualizacaoListaAsync(listaId, evento, usuarioId, cancellationToken);

    private static int MesclarDuplicados(ListaCompra lista, int usuarioId)
    {
        var removidos = 0;
        var grupos = lista.Itens
            .GroupBy(x => new { x.DescricaoNormalizada, x.Unidade })
            .Where(x => x.Count() > 1)
            .ToArray();

        foreach (var grupo in grupos)
        {
            var principal = grupo.First();
            foreach (var duplicado in grupo.Skip(1).ToArray())
            {
                principal.Quantidade += duplicado.Quantidade;
                if (!principal.PrecoUnitario.HasValue && duplicado.PrecoUnitario.HasValue)
                    principal.PrecoUnitario = duplicado.PrecoUnitario;
                if (string.IsNullOrWhiteSpace(principal.Observacao) && !string.IsNullOrWhiteSpace(duplicado.Observacao))
                    principal.Observacao = duplicado.Observacao;
                if (string.IsNullOrWhiteSpace(principal.EtiquetaCor) && !string.IsNullOrWhiteSpace(duplicado.EtiquetaCor))
                    principal.EtiquetaCor = duplicado.EtiquetaCor;
                principal.Comprado = principal.Comprado || duplicado.Comprado;
                principal.DataHoraCompra = principal.Comprado ? (principal.DataHoraCompra ?? duplicado.DataHoraCompra ?? DateTime.UtcNow) : null;
                principal.UsuarioCadastroId = usuarioId;
                AtualizarValorTotalItem(principal);
                DesvincularLogsDosItens(lista, new[] { duplicado.Id });
                lista.Itens.Remove(duplicado);
                removidos++;
            }
        }

        return removidos;
    }

    private static void DesvincularLogsDosItens(ListaCompra lista, IReadOnlyCollection<long> itensIds)
    {
        if (itensIds.Count == 0)
            return;

        var itensIdsSet = itensIds.ToHashSet();
        foreach (var log in lista.Logs.Where(x => x.ItemListaCompraId.HasValue && itensIdsSet.Contains(x.ItemListaCompraId.Value)))
            log.ItemListaCompraId = null;
    }

    private static void RegistrarHistoricoSePossuirPreco(
        int usuarioId,
        ItemListaCompra item,
        Produto produto,
        OrigemPrecoHistoricoCompra origem)
    {
        if (!item.PrecoUnitario.HasValue || item.PrecoUnitario.Value <= 0)
            return;

        produto.UltimoPrecoUnitario = item.PrecoUnitario.Value;
        produto.DataHoraUltimoPreco = DateTime.UtcNow;
        produto.HistoricosPreco.Add(new HistoricoProduto
        {
            UsuarioCadastroId = usuarioId,
            ItemListaCompra = item,
            Unidade = item.Unidade,
            PrecoUnitario = item.PrecoUnitario.Value,
            Origem = origem
        });
    }

    private async Task AtualizarParticipantesAsync(
        ListaCompra lista,
        int usuarioId,
        int usuarioReferenciaAmizadeId,
        IReadOnlyCollection<ParticipanteListaCompraRequest>? participantes,
        bool definirParticipantesPadraoQuandoNulo,
        bool exigirProprietarioAutenticado,
        CancellationToken cancellationToken)
    {
        if (participantes is null && !definirParticipantesPadraoQuandoNulo)
            return;

        var participantesSolicitados = participantes?.ToArray() ??
            [new ParticipanteListaCompraRequest(usuarioId, PapelParticipacaoListaCompra.Proprietario)];

        if (participantesSolicitados.Length == 0)
            throw new DomainException("lista_compra_proprietario_invalido");

        if (participantesSolicitados.Any(x => x.UsuarioId <= 0))
            throw new DomainException("participante_invalido");

        var usuariosDuplicados = participantesSolicitados
            .GroupBy(x => x.UsuarioId)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();
        if (usuariosDuplicados.Length > 0)
            throw new DomainException("participante_duplicado");

        var proprietarios = participantesSolicitados
            .Where(x => x.Papel == PapelParticipacaoListaCompra.Proprietario)
            .ToArray();
        if (proprietarios.Length != 1)
            throw new DomainException("lista_compra_proprietario_invalido");

        var novoProprietarioId = proprietarios[0].UsuarioId;
        if (exigirProprietarioAutenticado && novoProprietarioId != usuarioId)
            throw new DomainException("lista_compra_proprietario_invalido");

        var amigosIds = (await amizadeRepository.ListarIdsAmigosAceitosAsync(usuarioReferenciaAmizadeId, cancellationToken)).ToHashSet();
        foreach (var participante in participantesSolicitados.Where(x => x.UsuarioId != usuarioReferenciaAmizadeId))
        {
            if (!amigosIds.Contains(participante.UsuarioId))
                throw new DomainException("participante_nao_eh_amigo_aceito");
        }

        var solicitadosPorUsuarioId = participantesSolicitados.ToDictionary(x => x.UsuarioId, x => x);
        var existentesPorUsuarioId = lista.Participantes.ToDictionary(x => x.UsuarioId, x => x);

        foreach (var solicitacao in participantesSolicitados)
        {
            if (existentesPorUsuarioId.TryGetValue(solicitacao.UsuarioId, out var participacaoExistente))
            {
                participacaoExistente.Status = true;
                participacaoExistente.Papel = solicitacao.Papel;
                participacaoExistente.UsuarioCadastroId = usuarioId;
                continue;
            }

            lista.Participantes.Add(new ParticipacaoListaCompra
            {
                UsuarioCadastroId = usuarioId,
                UsuarioId = solicitacao.UsuarioId,
                Papel = solicitacao.Papel,
                Status = true
            });
        }

        foreach (var participacaoExistente in lista.Participantes.Where(x => !solicitadosPorUsuarioId.ContainsKey(x.UsuarioId)))
        {
            participacaoExistente.Status = false;
            participacaoExistente.UsuarioCadastroId = usuarioId;
        }

        lista.UsuarioProprietarioId = novoProprietarioId;
    }

    private async Task<ListaCompraDetalheDto> MapDetalheAsync(ListaCompra lista, int usuarioId, CancellationToken cancellationToken)
    {
        var usuariosPorId = (await usuarioRepository.ListarAtivosAsync(cancellationToken)).ToDictionary(x => x.Id, x => x);
        var participantes = MapParticipantesDetalhe(lista, usuariosPorId);

        var resumo = MapResumo(lista, usuarioId);
        return new ListaCompraDetalheDto(
            lista.Id,
            lista.Nome,
            lista.Categoria,
            lista.Observacao,
            lista.Status.ToString().ToLowerInvariant(),
            resumo.ValorTotal,
            resumo.ValorComprado,
            resumo.PercentualComprado,
            resumo.QuantidadeItens,
            resumo.QuantidadeItensComprados,
            lista.Itens
                .OrderBy(x => x.Descricao)
                .Select(MapItem)
                .ToArray(),
            participantes,
            lista.Logs
                .OrderByDescending(x => x.DataHoraCadastro)
                .Select(x => new ListaCompraLogDto(x.Id, x.DataHoraCadastro, x.UsuarioCadastroId, x.ItemListaCompraId, x.Acao, x.Descricao, x.ValorAnterior, x.ValorNovo))
                .ToArray(),
            lista.DataHoraAtualizacao);
    }

    private async Task<ListaCompraParticipantesDetalheDto> MapDetalheParticipantesAsync(ListaCompra lista, CancellationToken cancellationToken)
    {
        var usuariosPorId = (await usuarioRepository.ListarAtivosAsync(cancellationToken)).ToDictionary(x => x.Id, x => x);
        var participantes = MapParticipantesResumo(lista, usuariosPorId);
        var logs = lista.Logs
            .OrderByDescending(x => x.DataHoraCadastro)
            .Select(x => new ListaCompraLogDto(x.Id, x.DataHoraCadastro, x.UsuarioCadastroId, x.ItemListaCompraId, x.Acao, x.Descricao, x.ValorAnterior, x.ValorNovo))
            .ToArray();

        return new ListaCompraParticipantesDetalheDto(
            lista.Id,
            lista.Nome,
            lista.Categoria,
            lista.Observacao,
            lista.Status.ToString().ToLowerInvariant(),
            participantes,
            logs,
            lista.DataHoraAtualizacao);
    }

    private static IReadOnlyCollection<ParticipanteListaCompraDto> MapParticipantesDetalhe(
        ListaCompra lista,
        IReadOnlyDictionary<int, Core.Domain.Entities.Administracao.Usuario> usuariosPorId)
    {
        return lista.Participantes
            .Where(x => x.Status)
            .GroupBy(x => x.UsuarioId)
            .Select(x => x.OrderBy(p => p.Papel).First())
            .Select(x =>
            {
                usuariosPorId.TryGetValue(x.UsuarioId, out var usuario);
                return new ParticipanteListaCompraDto(
                    x.UsuarioId,
                    usuario?.Nome ?? $"Usuario {x.UsuarioId}",
                    usuario?.Email ?? string.Empty,
                    x.Papel.ToString().ToLowerInvariant());
            })
            .OrderBy(x => x.Nome)
            .ToArray();
    }

    private static IReadOnlyCollection<ParticipanteListaCompraResumoDto> MapParticipantesResumo(
        ListaCompra lista,
        IReadOnlyDictionary<int, Core.Domain.Entities.Administracao.Usuario> usuariosPorId)
    {
        return lista.Participantes
            .Where(x => x.Status)
            .GroupBy(x => x.UsuarioId)
            .Select(x => x.OrderBy(p => p.Papel).First())
            .Select(x =>
            {
                usuariosPorId.TryGetValue(x.UsuarioId, out var usuario);
                return new ParticipanteListaCompraResumoDto(
                    x.UsuarioId,
                    usuario?.Nome ?? $"Usuario {x.UsuarioId}",
                    x.Papel.ToString());
            })
            .OrderBy(x => x.Nome)
            .ToArray();
    }

    private static ItemListaCompraDto MapItem(ItemListaCompra item) =>
        new(
            item.Id,
            item.Descricao,
            item.Observacao,
            item.Unidade,
            item.Quantidade,
            item.PrecoUnitario,
            item.ValorTotal,
            item.EtiquetaCor,
            item.Comprado,
            item.DataHoraCompra);

    private static DesejoCompraDto MapDesejo(DesejoCompra desejo) =>
        new(
            desejo.Id,
            desejo.Descricao,
            desejo.Observacao,
            desejo.Unidade,
            desejo.Quantidade,
            desejo.PrecoEstimado,
            desejo.Convertido,
            desejo.DataHoraConversao);

    private static ListaCompraResumoDto MapResumo(ListaCompra lista, int usuarioId)
    {
        var quantidadeItens = lista.Itens.Count;
        var quantidadeComprados = lista.Itens.Count(x => x.Comprado);
        var valorTotal = lista.Itens.Sum(x => x.ValorTotal);
        var valorComprado = lista.Itens.Where(x => x.Comprado).Sum(x => x.ValorTotal);
        var percentualComprado = quantidadeItens == 0 ? 0m : decimal.Round((quantidadeComprados * 100m) / quantidadeItens, 2);
        var participantesAtivos = lista.Participantes.Count(x => x.Status);

        return new ListaCompraResumoDto(
            lista.Id,
            lista.Nome,
            lista.Categoria,
            lista.Observacao,
            lista.Status.ToString().ToLowerInvariant(),
            ResolverPapelUsuario(lista, usuarioId),
            valorTotal,
            valorComprado,
            percentualComprado,
            quantidadeItens,
            quantidadeComprados,
            participantesAtivos,
            lista.DataHoraAtualizacao);
    }

    private static string ResolverPapelUsuario(ListaCompra lista, int usuarioId)
    {
        if (lista.UsuarioProprietarioId == usuarioId)
            return PapelParticipacaoListaCompra.Proprietario.ToString();

        var participacao = lista.Participantes
            .Where(x => x.UsuarioId == usuarioId && x.Status)
            .OrderBy(x => x.Papel)
            .FirstOrDefault();

        return participacao is null
            ? PapelParticipacaoListaCompra.Leitor.ToString()
            : participacao.Papel.ToString();
    }

    private static ListaCompraLog CriarLog(int usuarioId, long? itemId, AcaoLogs acao, string descricao, string? valorAnterior = null, string? valorNovo = null) =>
        new()
        {
            UsuarioCadastroId = usuarioId,
            ItemListaCompraId = itemId,
            Acao = acao,
            Descricao = descricao,
            ValorAnterior = valorAnterior,
            ValorNovo = valorNovo
        };

    private static void AtualizarValorTotalItem(ItemListaCompra item)
    {
        if (!item.PrecoUnitario.HasValue || item.PrecoUnitario.Value <= 0 || item.Quantidade <= 0)
        {
            item.ValorTotal = 0m;
            return;
        }

        item.ValorTotal = decimal.Round(item.Quantidade * item.PrecoUnitario.Value, 2);
    }

    private static string NormalizarTextoObrigatorio(string? valor, string codigoErro)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DomainException(codigoErro);

        return valor.Trim();
    }

    private static string? NormalizarTextoOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string NormalizarDescricao(string valor) =>
        valor.Trim().ToLowerInvariant();

    private static void ValidarQuantidade(decimal quantidade)
    {
        if (quantidade <= 0)
            throw new DomainException("quantidade_item_invalida");
    }

    private static bool PodeVisualizar(ListaCompra lista, int usuarioId) =>
        lista.UsuarioProprietarioId == usuarioId || lista.Participantes.Any(x => x.UsuarioId == usuarioId && x.Status);

    private static bool PodeEditar(ListaCompra lista, int usuarioId)
    {
        if (lista.UsuarioProprietarioId == usuarioId)
            return true;

        return lista.Participantes.Any(x =>
            x.UsuarioId == usuarioId &&
            x.Status &&
            x.Papel != PapelParticipacaoListaCompra.Leitor);
    }

    private int ObterUsuarioAutenticadoId() =>
        usuarioAutenticadoProvider.ObterUsuarioId() ?? throw new DomainException("usuario_nao_autenticado");
}


