using System;
using System.Collections.Generic;
using UnityEngine;

public static class UiText
{
    private const string LegacyLanguageIndexKey = "MainMenu.Language";
    private const string SelectedLanguageKey = "SelectedLanguage";

    private static readonly Dictionary<string, string> PtToEn = new(StringComparer.Ordinal)
    {
        ["Este slot j\u00e1 tem um jogo guardado. Queres apagar este progresso e come\u00e7ar um novo jogo?"] = "This slot already has a saved game. Do you want to erase this progress and start a new game?",
        ["Queres apagar este jogo guardado deste slot?"] = "Do you want to delete the saved game in this slot?",
        ["Quando vires este circulo a rodar, o jogo est\u00e1 a guardar automaticamente"] = "When you see this circle spinning, the game is saving automatically",
        ["CLIQUE EM QUALQUER TELA PARA COMECAR"] = "CLICK ANYWHERE TO START",
        ["PRESS SPACE PARA CONTINUAR"] = "PRESS SPACE TO CONTINUE",
        ["Aprende as teclas antes de entrar no jogo."] = "Learn the keys before entering the game.",
        ["ANTES DE COME\u00c7AR"] = "BEFORE YOU START",
        ["ANTES DE COMECAR"] = "BEFORE YOU START",
        ["PO\u00c7\u00c3O DE CURA PEQUENA"] = "SMALL HEALING POTION",
        ["PO\u00c7\u00c3O DE CURA GRANDE"] = "LARGE HEALING POTION",
        ["PO\u00c7\u00c3O PEQUENA"] = "SMALL POTION",
        ["PO\u00c7\u00c3O GRANDE"] = "LARGE POTION",
        ["ANDAR PARA A FRENTE"] = "MOVE FORWARD",
        ["ANDAR PARA A ESQUERDA"] = "MOVE LEFT",
        ["ANDAR PARA TR\u00c1S"] = "MOVE BACK",
        ["ANDAR PARA TRAS"] = "MOVE BACK",
        ["ANDAR PARA A DIREITA"] = "MOVE RIGHT",
        ["Nenhuma op\u00e7\u00e3o dispon\u00edvel por agora."] = "No options available right now.",
        ["O vinho esconde verdades que a mente nao quer ver."] = "Wine hides truths the mind does not want to see.",
        ["O vinho esconde verdades que a mente n\u00e3o quer ver."] = "Wine hides truths the mind does not want to see.",
        ["DICA: Ouve os sons \u00e0 tua volta. Cada detalhe pode esconder uma pista."] = "TIP: Listen to the sounds around you. Every detail may hide a clue.",
        ["DESENVOLVIDO POR"] = "DEVELOPED BY",
        ["Jogo guardado"] = "Saved Game",
        ["Slot vazio"] = "Empty Slot",
        ["Continuar Jogo"] = "Continue Game",
        ["CONTINUAR JOGO"] = "CONTINUE GAME",
        ["NOVO JOGO"] = "NEW GAME",
        ["Novo Jogo"] = "New Game",
        ["CONTINUAR"] = "CONTINUE",
        ["OP\u00c7\u00d5ES"] = "OPTIONS",
        ["OPCOES"] = "OPTIONS",
        ["CR\u00c9DITOS"] = "CREDITS",
        ["CREDITOS"] = "CREDITS",
        ["SAIR SEM GUARDAR"] = "EXIT WITHOUT SAVING",
        ["SAIR"] = "EXIT",
        ["VOLTAR"] = "BACK",
        ["APLICAR"] = "APPLY",
        ["REPOR"] = "RESET",
        ["CANCELAR"] = "CANCEL",
        ["SIM, APAGAR"] = "YES, DELETE",
        ["ESCOLHER SLOT"] = "CHOOSE SLOT",
        ["IDIOMA"] = "LANGUAGE",
        ["Taverna do Baco"] = "Bacchus Tavern",
        ["Comida, Bebida e Boas Historias"] = "Food, Drink and Good Stories",
        ["Item"] = "Item",
        ["Descricao"] = "Description",
        ["Preço"] = "Price",
        ["Preco"] = "Price",
        ["[ESC] Voltar"] = "[ESC] Back",
        ["Inventario nao encontrado."] = "Inventory not found.",
        ["Ja tens o maximo de 5 dessa garrafa."] = "You already have the maximum of 5 of this bottle.",
        ["Nao tens moedas suficientes."] = "You do not have enough coins.",
        ["CHEIO"] = "FULL",
        ["Cada garrafa pode ir ate 5 no inventario."] = "You can carry up to 5 of each bottle in your inventory.",
        ["JOGO GUARDADO COM SUCESSO."] = "GAME SAVED SUCCESSFULLY.",
        ["JOGO GUARDADO"] = "GAME SAVED",
        ["Existem alteracoes por guardar. Deseja sair sem guardar as alteracoes?"] = "There are unsaved changes. Do you want to leave without saving them?",
        ["Abrir Loja"] = "Open Shop",
        ["Vinho da Vida"] = "Wine of Life",
        ["Bencao de Baco"] = "Blessing of Bacchus",
        ["Pao Rustico"] = "Rustic Bread",
        ["Ensopado de Carne"] = "Meat Stew",
        ["Perna de Porco Assada"] = "Roasted Pork Leg",
        ["Queijo Curado"] = "Aged Cheese",
        ["Vinho doce guardado em garrafa lacrada a cera. Restaura um pouco de Vida."] = "Sweet wine sealed in wax. Restores a small amount of Health.",
        ["A lendaria bebida divina de tom rubi. Restaura a Saude completamente."] = "The legendary divine ruby drink. Restores Health completely.",
        ["Pao fresco e simples, feito com farinha de trigo. Restaura uma pequena quantidade de Vida."] = "Fresh, simple bread made with wheat flour. Restores a small amount of Health.",
        ["Ensopado quente e nutritivo, preparado com carne e legumes da estacao. Restaura uma boa quantidade de Vida."] = "A warm, nutritious stew made with meat and seasonal vegetables. Restores a good amount of Health.",
        ["Carne suculenta e bem temperada, assada lentamente. Restaura uma grande quantidade de Vida."] = "Juicy, well-seasoned meat roasted slowly. Restores a large amount of Health.",
        ["Queijo curado, forte e saboroso. Restaura um pouco de Vida."] = "Aged cheese, strong and flavorful. Restores a small amount of Health.",
        ["PORTUGU\u00caS"] = "PORTUGUESE",
        ["PORTUGUES"] = "PORTUGUESE",
        ["INGL\u00caS"] = "ENGLISH",
        ["LIGADO"] = "ON",
        ["DESLIGADO"] = "OFF",
        ["LEGENDAS"] = "SUBTITLES",
        ["MODO DE \u00c1UDIO"] = "AUDIO MODE",
        ["MODO DE AUDIO"] = "AUDIO MODE",
        ["VOLUME GERAL"] = "MASTER VOLUME",
        ["VOLUME DA M\u00daSICA"] = "MUSIC VOLUME",
        ["VOLUME DA MUSICA"] = "MUSIC VOLUME",
        ["VOLUME DOS EFEITOS"] = "SFX VOLUME",
        ["\u00c1UDIO"] = "AUDIO",
        ["AUDIO"] = "AUDIO",
        ["JOGABILIDADE"] = "GAMEPLAY",
        ["JOGO"] = "GAME",
        ["INTERFACE"] = "INTERFACE",
        ["CONTROLOS"] = "CONTROLS",
        ["CONTROLES"] = "CONTROLS",
        ["GR\u00c1FICOS"] = "GRAPHICS",
        ["GRAFICOS"] = "GRAPHICS",
        ["A CARREGAR..."] = "LOADING...",
        ["AUTOSAVE"] = "AUTOSAVE",
        ["MOVIMENTO"] = "MOVEMENT",
        ["A\u00c7\u00d5ES"] = "ACTIONS",
        ["ACOES"] = "ACTIONS",
        ["ANDAR"] = "MOVE",
        ["CORRER"] = "RUN",
        ["SALTAR"] = "JUMP",
        ["ROLAR"] = "ROLL",
        ["REBOLAR"] = "ROLL",
        ["INTERAGIR"] = "INTERACT",
        ["INTERA\u00c7\u00c3O"] = "INTERACTION",
        ["INTERACAO"] = "INTERACTION",
        ["ATAQUE PESADO"] = "HEAVY ATTACK",
        ["ATAQUE"] = "ATTACK",
        ["DEFENDER"] = "DEFEND",
        ["PAUSA"] = "PAUSE",
        ["Data:"] = "Date:",
        ["Scene:"] = "Scene:",
        ["Tempo:"] = "Time:",
        ["Vazio"] = "Empty",
        ["PRIME UMA TECLA"] = "PRESS A KEY"
        ,["Apanhar garrafa"] = "Pick up bottle"
        ,["Entrar portal"] = "Enter portal"
        ,["Clica F para entrar"] = "Enter"
        ,["Nivel concluido"] = "Level completed"
        ,["Pressiona F para interagir"] = "Press F to interact"
        ,["Pressiona {0} para interagir"] = "Press {0} to interact"
        ,["Interagir"] = "Interact"
        ,["GUARDAR"] = "SAVE"
        ,["DEFINIÇÕES"] = "SETTINGS"
        ,["DEFINICOES"] = "SETTINGS"
        ,["PAUSE"] = "PAUSE"
        ,["O Ébrio Corrompido"] = "The Corrupted Drunkard"
        ,["O Bode Viciado"] = "The Addicted Goat"
        ,["O Rei da Embriaguez"] = "The King of Drunkenness"
        ,["Guarda o jogo regularmente — um progresso perdido é uma aventura perdida."] = "Save regularly — lost progress means a lost adventure."
        ,["Explora cada canto da masmorra. Os melhores tesouros escondem-se nos lugares mais sombrios."] = "Explore every corner of the dungeon. The best treasures hide in the darkest places."
        ,["Os inimigos ficam mais perigosos à medida que avanças. Prepara-te antes de mergulhares fundo."] = "Enemies grow more dangerous as you advance. Prepare yourself before diving deeper."
        ,["A tua saúde não se regenera sozinha. Usa as poções com sabedoria."] = "Your health does not regenerate on its own. Use potions wisely."
        ,["Observa os padrões de ataque dos inimigos antes de te lançares ao combate."] = "Observe enemy attack patterns before throwing yourself into combat."
        ,["O mercador pode ter exactamente o que precisas para sobreviver à próxima sala."] = "The merchant may have exactly what you need to survive the next room."
        ,["Nem todos os confrontos precisam de ser ganhos pela força bruta."] = "Not every confrontation needs to be won through brute force."
        ,["Um golpe bem cronometrado vale mais do que cem ataques precipitados."] = "A well-timed strike is worth more than a hundred reckless attacks."
        ,["A escuridão da masmorra esconde tanto perigo como recompensa."] = "The darkness of the dungeon hides as much danger as reward."
        ,["Cada morte é uma lição. Os mais sábios aprendem com ela."] = "Every death is a lesson. The wisest learn from it."
        ,["Mantém o inventário organizado — nunca sabes quando vais precisar de um item urgente."] = "Keep your inventory organized — you never know when you'll urgently need an item."
        ,["Os chefes têm padrões previsíveis. Estuda os seus movimentos antes de atacar."] = "Bosses have predictable patterns. Study their moves before attacking."
        ,["A paciência é uma virtude no combate. Espera sempre pelo momento certo."] = "Patience is a virtue in combat. Always wait for the right moment."
        ,["Dinheiro bem gasto no mercador pode ser a diferença entre a vida e a morte."] = "Money well spent at the merchant can mean the difference between life and death."
        ,["Atenção aos detalhes do ambiente — podem indicar armadilhas à frente."] = "Pay attention to environmental details — they may indicate traps ahead."
        ,["Recolhe tudo o que encontrares. Nunca sabes o que pode ser útil mais tarde."] = "Collect everything you find. You never know what might be useful later."
        ,["A beco guarda segredos que apenas os mais atentos conseguem descobrir."] = "The alley holds secrets that only the most observant can uncover."
        ,["O teu personagem tem mais força do que imaginas — aprende a dominá-lo."] = "Your character has more strength than you imagine — learn to master it."
        ,["Alguns inimigos são melhor evitados do que confrontados directamente."] = "Some enemies are better avoided than confronted directly."
        ,["A masmorra tem memória — os teus actos têm consequências."] = "The dungeon has memory — your actions have consequences."
    };

    private static readonly Dictionary<string, string> EnToPt = BuildReverseMap(PtToEn);

    public static LanguageManager.Language CurrentLanguage
    {
        get
        {
            if (LanguageManager.Instance != null)
            {
                return LanguageManager.Instance.CurrentLanguage;
            }

            if (PlayerPrefs.HasKey(SelectedLanguageKey))
            {
                string savedLanguage = PlayerPrefs.GetString(SelectedLanguageKey, "PT");
                if (Enum.TryParse(savedLanguage, out LanguageManager.Language parsed))
                {
                    return parsed;
                }
            }

            int languageIndex = PlayerPrefs.GetInt(LegacyLanguageIndexKey, 0);
            return LanguageFromIndex(languageIndex);
        }
    }

    public static bool IsPortuguese => CurrentLanguage == LanguageManager.Language.PT;

    public static string Choose(string portuguese, string english)
    {
        return IsPortuguese ? portuguese : english;
    }

    public static LanguageManager.Language LanguageFromIndex(int index)
    {
        return index <= 0 ? LanguageManager.Language.PT : LanguageManager.Language.EN;
    }

    public static int IndexFromLanguage(LanguageManager.Language language)
    {
        return language == LanguageManager.Language.PT ? 0 : 1;
    }

    public static string LocalizeKnownText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        Dictionary<string, string> map = IsPortuguese ? EnToPt : PtToEn;
        return map.TryGetValue(value, out string localized) ? localized : value;
    }

    public static string GetMainMenuButtonLabel(string buttonName)
    {
        return buttonName switch
        {
            "new" => Choose("NOVO JOGO", "NEW GAME"),
            "continue" => Choose("CONTINUAR", "CONTINUE"),
            "options" => Choose("OP\u00c7\u00d5ES", "OPTIONS"),
            "credits" => Choose("CR\u00c9DITOS", "CREDITS"),
            "exit" => Choose("SAIR", "EXIT"),
            "back" => Choose("VOLTAR", "BACK"),
            "apply" => Choose("APLICAR", "APPLY"),
            "reset" => Choose("REPOR", "RESET"),
            "delete" => Choose("APAGAR", "DELETE"),
            "confirm" => Choose("CONFIRMAR", "CONFIRM"),
            "cancel" => Choose("CANCELAR", "CANCEL"),
            _ => buttonName
        };
    }

    public static string GetOptionsSectionTitle(int index)
    {
        return index switch
        {
            0 => Choose("JOGO", "GAME"),
            1 => Choose("GR\u00c1FICOS", "GRAPHICS"),
            2 => Choose("\u00c1UDIO", "AUDIO"),
            3 => Choose("CONTROLOS", "CONTROLS"),
            4 => Choose("IDIOMA", "LANGUAGE"),
            _ => string.Empty
        };
    }

    public static string GetSelectorLabel(string selectorName, int index)
    {
        return selectorName switch
        {
            "audio" => index == 0 ? Choose("EST\u00c9REO", "STEREO") : "MONO",
            "subtitles" => index == 0 ? Choose("LIGADO", "ON") : Choose("DESLIGADO", "OFF"),
            "language" => index == 0 ? Choose("PORTUGU\u00caS", "PORTUGUESE") : "ENGLISH",
            _ => string.Empty
        };
    }

    public static string GetBindingActionLabel(MovementKeyBindings.GameAction action)
    {
        return action switch
        {
            MovementKeyBindings.GameAction.Forward => Choose("ANDAR PARA A FRENTE", "MOVE FORWARD"),
            MovementKeyBindings.GameAction.Left => Choose("ANDAR PARA A ESQUERDA", "MOVE LEFT"),
            MovementKeyBindings.GameAction.Backward => Choose("ANDAR PARA TR\u00c1S", "MOVE BACK"),
            MovementKeyBindings.GameAction.Right => Choose("ANDAR PARA A DIREITA", "MOVE RIGHT"),
            MovementKeyBindings.GameAction.Run => Choose("CORRER", "RUN"),
            MovementKeyBindings.GameAction.Interact => Choose("INTERAGIR", "INTERACT"),
            MovementKeyBindings.GameAction.Attack => Choose("ATAQUE", "ATTACK"),
            MovementKeyBindings.GameAction.HeavyAttack => Choose("ATAQUE PESADO", "HEAVY ATTACK"),
            MovementKeyBindings.GameAction.Defend => Choose("DEFENDER", "DEFEND"),
            MovementKeyBindings.GameAction.Jump => Choose("SALTAR", "JUMP"),
            MovementKeyBindings.GameAction.Roll => Choose("ROLAR", "ROLL"),
            _ => string.Empty
        };
    }

    private static Dictionary<string, string> BuildReverseMap(Dictionary<string, string> source)
    {
        Dictionary<string, string> reverse = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> pair in source)
        {
            if (!reverse.ContainsKey(pair.Value))
            {
                reverse.Add(pair.Value, pair.Key);
            }
        }

        return reverse;
    }
}
