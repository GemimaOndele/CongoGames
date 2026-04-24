using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using CongoGames.Presentation;

namespace CongoGames.Editor
{
    public static class ThemeModeFoldersMenu
    {
        private const string ReadmeName = "LISEZMOI_fond_video.txt";

        [MenuItem("CongoGames/Thème/Créer dossiers fond vidéo par mode (StreamingAssets)")]
        public static void CreateModeFolders()
        {
            string theme = Path.Combine(Application.dataPath, "StreamingAssets", "Theme");
            if (!Directory.Exists(theme))
            {
                Directory.CreateDirectory(theme);
            }

            int n = 0;
            foreach (string id in ThemeModeCatalog.AllModeIds)
            {
                string dir = Path.Combine(theme, id);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string readme = Path.Combine(dir, ReadmeName);
                if (!File.Exists(readme))
                {
                    File.WriteAllText(readme, BuildReadme(id), new UTF8Encoding(false));
                }

                n++;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "CongoGames — Thème",
                n + " dossier(s) vérifiés sous Assets/StreamingAssets/Theme/<mode>.\n" +
                "Placez un fond animé (loop) : " + string.Join(", ", ThemeModeCatalog.BackgroundVideoFileNames) + "\n" +
                "Voir docs/THEME_BACKGROUNDS.md",
                "OK");
        }

        private static string BuildReadme(string modeId)
        {
            var sb = new StringBuilder(512);
            sb.Append("Fond d'écran — mode : ").AppendLine(modeId).AppendLine();
            sb.AppendLine("Placez ici UNE de ces vidéos en boucle (H.264 .mp4 ou .webm) :");
            foreach (string f in ThemeModeCatalog.BackgroundVideoFileNames)
            {
                sb.AppendLine("  - " + f);
            }

            sb.AppendLine();
            sb.AppendLine("Priorité d'affichage dans le jeu :");
            sb.AppendLine("1) URL dans Theme/remote_media.json (fichier + HTTPS direct)");
            sb.AppendLine("2) Cette vidéo locale (premier nom trouvé ci-dessus)");
            sb.AppendLine("3) Sinon plateau 3D animé (VirtualShowStage) si CongoUseVirtual3D=1");
            sb.AppendLine("4) Sinon fond synthétique 2D (bandes)");
            sb.AppendLine();
            sb.AppendLine("Détails : docs/THEME_BACKGROUNDS.md (dépôt racine).");
            return sb.ToString();
        }
    }
}
