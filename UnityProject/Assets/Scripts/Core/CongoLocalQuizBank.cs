using System.Collections.Generic;
using UnityEngine;

namespace CongoGames.Core
{
    /// <summary>
    /// Questions culture Congo + vocabulaire (inspiré des jeux de lettres type Tusmo / AfroTusmo).
    /// Tirage aléatoire sans répétition jusqu’à épuisement de la banque, puis nouveau mélange.
    /// </summary>
    public static class CongoLocalQuizBank
    {
        private struct QTemplate
        {
            public string question;
            public string a;
            public string b;
            public string c;
            public string d;
            public int correctIndex;
            public string category;
        }

        private static readonly QTemplate[] Templates =
        {
            T("Quelle est la capitale politique de la République du Congo (Brazzaville) ?", "Brazzaville", "Libreville", "Pointe-Noire", "Malabo", 0, "géographie"),
            T("Couleurs du drapeau du Congo (bandes) ?", "Vert, jaune, rouge", "Bleu, blanc, rouge", "Noir, jaune, rouge", "Orange, blanc, vert", 0, "symboles"),
            T("Quel fleuve majeur traverse le pays et baigne Brazzaville ?", "Le fleuve Congo", "Le Nil", "Le Niger", "Le Zambèze", 0, "géographie"),
            T("Dans l’usage courant, « le Congo » sans précision désigne ici le pays dont la capitale est …", "Brazzaville", "Kinshasa (RDC)", "Libreville", "Yaoundé", 0, "géographie"),
            T("La République démocratique du Congo (RDC) se distingue du Congo par le fait que sa capitale est …", "Kinshasa", "Brazzaville", "Lubumbashi (seulement)", "Pointe-Noire", 0, "géographie"),
            T("En lingala, « mbote » (selon contexte) sert surtout à …", "Saluer / dire bonjour", "Commander de l’eau", "Dire l’heure", "Dire « au revoir » seulement", 0, "langues"),
            T("En kituba, « santu » correspond souvent à …", "Saint / sainte (titre chrétiens)", "La pluie", "Le soleil seul", "Un fruit", 0, "langues"),
            T("En kituba, « malamu » (bien) s’emploie le plus souvent pour …", "Dire qu’on va bien / un accord simple, « c’est bon »", "Compter des kilomètres", "Dire l’heure en format 24h", "Nommer un mois lunaire chinois", 0, "langues"),
            T("Langues très présentes au Congo-Brazzaville ?", "Français, Lingala, Kituba", "Anglais seul", "Arabe, swahili", "Portugais, espagnol", 0, "langues"),
            T("Le pays dont la capitale est Brazzaville se situe sur quel continent ?", "Afrique", "Europe", "Asie", "Amérique du Sud", 0, "géographie"),
            T("Brazzaville se trouve sur quelle rive du fleuve Congo ?", "Rive nord", "Rive sud", "Rive est uniquement", "Loin du fleuve", 0, "géographie"),
            T("Océan à l’ouest du Congo ?", "Atlantique", "Indien", "Arctique", "Pacifique sud", 0, "géographie"),
            T("« Mbote » en lingala / kituba correspond souvent à …", "Salut / bonjour", "Merci", "Au revoir", "Eau", 0, "langues"),
            T("« Melesi » (variantes) exprime souvent …", "Merci", "Bonjour", "Excuse", "Feu", 0, "langues"),
            T("« Tatá » en lingala désigne souvent …", "Père", "Mère", "Frère", "Oiseau", 0, "langues"),
            T("« Mamá » en lingala désigne souvent …", "Mère", "Père", "Maison", "Route", 0, "langues"),
            T("« Mangi » évoque souvent l’action de …", "Manger", "Dormir", "Courir", "Chanter", 0, "langues"),
            T("« Zoba » en lingala signifie souvent …", "Idiot / naïf", "Sage", "Riche", "Vite", 0, "langues"),
            T("« Malamu » peut signifier …", "Bien / ça va", "Mauvais", "Froid", "Lundi", 0, "langues"),
            T("« Motuya » évoque souvent …", "Soif / avoir soif", "Faim", "Sommeil", "Peur", 0, "langues"),
            T("« Nzoto » désigne souvent …", "Le corps", "La maison", "La nuit", "Le ciel", 0, "langues"),
            T("« Maboko » désigne souvent …", "Les mains", "Les pieds", "Les yeux", "Les oreilles", 0, "langues"),
            T("« Mpela » peut désigner …", "Église / lieu de culte", "École", "Marché", "Stade", 0, "culture"),
            T("« Koluka » évoque souvent …", "Prier", "Manger", "Nager", "Danser seul", 0, "culture"),
            T("« Kokende » signifie souvent …", "Aller / marcher", "Manger", "Dormir", "Lire", 0, "langues"),
            T("« Kokosa » peut signifier …", "Appeler / crier", "Cuisiner", "Peindre", "Gagner", 0, "langues"),
            T("Département connu pour le pétrole et la côte ?", "Pointe-Noire", "Plateaux", "Cuvette", "Likouala", 0, "géographie"),
            T("Grande forêt tropicale au nord du pays ?", "Forêt du Congo", "Forêt amazonienne", "Taïga", "Brousse sahélienne", 0, "nature"),
            T("Rumba congolaise : elle est surtout associée à …", "Danse et musique urbaine", "Jazz US pur", "Opéra classique", "Reggae jamaïcain seul", 0, "musique"),
            T("Instrument à percussion très présent en fête ?", "Tambour / ngoma", "Harpe celtique", "Cornemuse", "Piano à queue seul", 0, "culture"),
            T("Savane vs forêt : le sud du pays est plutôt …", "Plus sec / savanes côtières", "Glacier", "Désert de sable pur", "Toundra", 0, "géographie"),
            T("Monnaie utilisée en République du Congo ?", "Franc CFA (CEMAC)", "Euro seul", "Dollar US officiel", "Livre sterling", 0, "économie"),
            T("CongoGames met en avant surtout …", "Culture et fierté du Congo", "Cuisine italienne", "Sports US", "Cinéma japonais", 0, "jeu"),
            T("Le département de la Cuvette est surtout connu pour …", "Fleuve, marécages, bassins versants (centre–nord)", "Des pistes de ski alpin", "Désert de sable", "Côte océanique seulement", 0, "géographie"),
            T("Un pays frontalier du Congo-Brazzaville ?", "Gabon", "Madagascar", "Sénégal", "Kenya", 0, "géographie"),
            T("Capitale économique côtière importante ?", "Pointe-Noire", "Ouesso", "Impfondo", "Madingou", 0, "géographie"),
            T("Parc connu pour les gorilles (nord) ?", "Nouabalé-Ndoki", "Yellowstone", "Kruger", "Serengeti", 0, "nature"),
            T("« Sango » est surtout une langue du …", "Voisin (Centrafrique)", "Congo uniquement", "Maroc", "Brésil", 0, "langues"),
            T("« Koko » en contexte familial peut dire …", "Grand-père (selon usage)", "Poisson", "Nuage", "Train", 0, "langues"),
            T("« Biso » correspond souvent à …", "Nous", "Vous (poli)", "Ils seulement", "Personne", 0, "langues"),
            T("« Yo » en interjection familière peut être …", "Salut / hey", "Non", "Merci", "Arrêt", 0, "langues"),
            T("« Te » en lingala peut marquer souvent …", "Négation (ne…pas)", "Futur", "Passé", "Couleur", 0, "langues"),
            T("« Motema » désigne souvent …", "Le cœur", "Le ventre", "Le dos", "Le genou", 0, "langues"),
            T("« Sanza » ou likembe : instrument à …", "Lames / idiophone", "Cordes seules", "Cuivre soufflé", "Électronique", 0, "musique"),
            T("Fête nationale (indépendance) commémorée le …", "15 août", "1er janvier", "4 juillet", "14 juillet seul", 0, "histoire"),
            T("Le fleuve Congo se jette dans …", "L’océan Atlantique", "La mer Rouge", "La mer Caspienne", "Le lac Victoria", 0, "géographie"),
            T("« Pool » désigne ici surtout …", "Une région / département", "Une piscine olympique", "Un jeu de billard US", "Un lac salé", 0, "géographie"),
            T("Brasseries / boisson locale très répandue : thème …", "Bière / boissons locales", "Saké", "Cidre breton", "Kvass", 0, "culture"),
            T("La ndombolo (danse) est surtout associée à …", "Musiques pop congolaises récentes", "Valse autrichienne", "Flamenco seul", "Haka", 0, "musique"),
            T("« Makossa » est surtout originaire …", "Cameroun (voisin culturel)", "Islande", "Japon", "Norvège", 0, "musique"),
            T("Pour dire « eau » on utilise souvent en lingala …", "Mai / maii (graphies variables)", "Moto", "Mwinda", "Nzete", 0, "langues"),
            T("« Mwinda » peut être …", "Lampe / lumière", "Eau", "Sel", "Route", 0, "langues"),
            T("« Nzela » signifie souvent …", "Chemin / route", "Nuage", "Étoile", "Pain", 0, "langues"),
            T("« Biloko » peut désigner …", "Choses / affaires", "Oiseaux", "Poissons", "Nuages", 0, "langues"),
            T("« Leka » peut signifier …", "Jouer", "Travailler la terre seul", "Payer l’impôt", "Réparer un toit", 0, "langues"),
            T("« Kozala » est souvent …", "Être / rester", "Partir vite", "Casser", "Vendre", 0, "langues"),
            T("« Kozwa » peut signifier …", "Recevoir / obtenir", "Donner", "Cacher", "Nager", 0, "langues"),
            T("En classe, on note souvent le cours dans un …", "Cahier", "Four", "Réfrigérateur", "Tapis", 0, "culture"),
            T("Sport collectif très populaire en Afrique et au Congo ?", "Football", "Curling", "Baseball", "Hockey sur glace", 0, "sport"),
            T("Brazzaville est souvent qualifiée de … sur le grand fleuve ?", "Grande « capitale bord de fleuve » (rive nord)", "Métropole arctique", "Port méditerranéen", "Base spatiale", 0, "géographie"),
            T("Le département de la Likouala est surtout connu pour …", "Ses forêts et cours d’eau du nord", "Ses pistes de ski", "Ses champs de lavande", "Ses volcans en activité", 0, "géographie"),
            T("Quel animal emblématique trouve-t-on dans les forêts du nord congolais ?", "Gorille / éléphant de forêt (selon zones)", "Pingouin empereur", "Kangourou", "Ours polaire", 0, "nature"),
            T("Le Pool (département) entoure surtout …", "Brazzaville et sa périphérie", "L’île de Groenland", "Le désert du Sahara central", "La cordillère des Andes", 0, "géographie"),
            T("Quel pays partage une frontière terrestre avec le Congo au sud-ouest ?", "Angola (Cabinda, enclave)", "Islande", "Japon", "Norvège", 0, "géographie"),
            T("En République du Congo, l’administration utilise encore beaucoup …", "Le français (langue officielle)", "Le japonais", "Le swahili seul", "Le latin classique", 0, "culture"),
            T("« Liboso » peut signifier souvent …", "Devant / auparavant", "Derrière uniquement", "Jamais", "Lundi", 0, "langues"),
            T("« Sima » peut désigner souvent …", "Derrière / le dos", "Devant", "Ciel", "Midi", 0, "langues"),
            T("« Mwana » désigne souvent …", "Enfant", "Grand-parent", "Chef de village seul", "Nuage", 0, "langues"),
            T("« Mosala » évoque souvent …", "Travail", "Sommeil", "Neige", "Train", 0, "langues"),
            T("« Mpiko » peut signifier …", "Courage / fierté", "Eau", "Sel", "Route", 0, "langues"),
            T("Le bassin du fleuve Congo concerne surtout quel continent ?", "Afrique", "Europe", "Australie", "Amérique du Nord", 0, "géographie"),
            T("Un plat à base de manioc râpé fermenté est souvent …", "Chikwangue / bâton de manioc", "Sushi", "Paella", "Raclette", 0, "culture"),
            T("La République du Congo a accédé à l’indépendance vis-à-vis de …", "La France (1960)", "Le Portugal (1822)", "Le Royaume-Uni (1776)", "L’Italie (1861)", 0, "histoire"),
            T("Quel océan borde Pointe-Noire ?", "Atlantique", "Indien", "Arctique", "Pacifique", 0, "géographie"),
            T("Le kwanga est surtout …", "Un aliment à base de manioc", "Un instrument à vent métallique", "Une danse bretonne", "Un jeu de cartes", 0, "culture"),
            T("Parmi ces danses, laquelle est liée au Congo dans l’imaginaire populaire ?", "Ndombolo / mouvements de hanches", "Flamenco seul", "Polka alpine", "Claquettes irlandaises", 0, "musique"),
            T("En blind test, on demande en général de deviner le … (plusieurs bons) ?", "Titre, l’artiste/groupe, ou le style", "Soleil, la lune, la marée seuls", "Prix d’un billet d’avion", "N° de châssis du micro", 0, "musique"),
            T("Un « orchestre à guitares » (soukous / rumba) met souvent en avant …", "Solo de guitare et chœurs", "Seulement de la flûte à bec", "Machine à café", "Chariot de golf", 0, "musique"),
            T("La « seben » (fin de morceau, solo, danse) est surtout associée à …", "L’enchaînement final des parties africaines pop", "Le 1er mouvement d’un requiem", "Le silence de 10 minutes", "Un match de snooker", 0, "musique"),
            T("Pour animer un live sur TikTok avec du Congo, on privilégie souvent …", "BPM souples, rythme dansant, titres mémorables", "Bruit blanc 20 min", "Opéra wagnérien intégral", "Conférence sur les impôts", 0, "musique"),
            T("Pointe-Noire surnommée en partie pour … ?", "Pétrole, port, océan", "Fêtes vénitiennes de la neige", "Forêt tropicale pure sans côte", "Volcan actif 4000 m", 0, "géographie"),
            T("Le département de la Cuvette est surtout plutôt …", "Forêts et zones humides intérieures", "Désert de glace", "Pôle industriel siderurgique alpin", "Archipel de 200 îles", 0, "géographie"),
            T("« Pool » côté populaire : on parle souvent du …", "Bassin autour de Brazzaville", "Océan Indien seul", "Désert d’Atacama", "Canal de Suez", 0, "géographie"),
            T("Un plat à base de feuille de manioc cuite : gombo / … ?", "Légumes locaux, sauce, accompagnement", "Sashimi de saumon norvégien", "Raclette végétalienne de ski", "Smoothie ananas uniquement", 0, "culture"),
            T("Très gros stade de football (près de Brazzaville, grands jeux) : on le nomme le plus souvent … ?", "Stade de Kintélé", "Stade de France (Paris)", "Santiago Bernabéu (Madrid)", "Old Trafford (Manchester)", 0, "sport"),
            T("Cathédrale de style néo-byzantin repérable à Brazzaville, dédiée à … (nom courant) ?", "Sainte-Anne (cathédrale de Brazzaville)", "Notre-Dame (Paris) copié", "Dôme d’Aix-la-Chapelle (titre court", "Cathédrale de Cologne (Allemagne)", 0, "culture"),
            T("Pour un live (TikTok), la priorité n°1 pour rester lisible, c’est d’abord : … ?", "Gros caractères, contraste, son clair, peu d’effets sur le texte", "Texte 6 pt sur bande claire seulement", "Caméra tournante rapide 360° seulement", "Silence 5 min d’ouverture", 0, "jeu"),
            T("Quand on distingue le Congo (capitale Brazzaville) de la RDC (capitale Kinshasa)…", "Sont deux pays distincts, deux capitales sur le fleuve, deux législations", "C’est le même drapeau exact", "Une seule monnaie officielle", "Même drapeau et même hymne seuls", 0, "culture")
        };

        private static readonly Queue<int> DrawQueue = new Queue<int>();
        private static int lastTemplateIndex = -1;

        private static QTemplate T(string q, string a, string b, string c, string d, int ok, string cat)
        {
            return new QTemplate
            {
                question = q,
                a = a,
                b = b,
                c = c,
                d = d,
                correctIndex = ok,
                category = cat
            };
        }

        private static void RefillQueue()
        {
            List<int> order = new List<int>(Templates.Length);
            for (int i = 0; i < Templates.Length; i++)
            {
                order.Add(i);
            }

            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            if (order.Count > 1 && lastTemplateIndex >= 0 && order[0] == lastTemplateIndex)
            {
                (order[0], order[1]) = (order[1], order[0]);
            }

            DrawQueue.Clear();
            foreach (int idx in order)
            {
                DrawQueue.Enqueue(idx);
            }
        }

        public static LiveQuestion PickRandom()
        {
            if (DrawQueue.Count == 0)
            {
                RefillQueue();
            }

            int ti = DrawQueue.Dequeue();
            lastTemplateIndex = ti;
            return FromTemplate(Templates[ti]);
        }

        private static LiveQuestion FromTemplate(QTemplate t)
        {
            string[] opts = { t.a, t.b, t.c, t.d };
            return NewShuffled(t.question, opts, t.correctIndex, t.category);
        }

        private static LiveQuestion NewShuffled(string q, string[] opts, int correctIdx, string category)
        {
            int n = opts.Length;
            int[] order = { 0, 1, 2, 3 };
            for (int i = n - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            string[] shuffled = new string[n];
            int newCorrect = 0;
            for (int i = 0; i < n; i++)
            {
                shuffled[i] = opts[order[i]];
                if (order[i] == correctIdx)
                {
                    newCorrect = i;
                }
            }

            string letter = ((char)('A' + newCorrect)).ToString();
            return new LiveQuestion
            {
                category = category,
                difficulty = "facile",
                question = q,
                options = shuffled,
                correctAnswer = letter,
                explanation = ""
            };
        }
    }
}
