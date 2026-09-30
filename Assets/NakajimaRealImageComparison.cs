using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Registers a real Nakajima photograph to the current synthetic camera image and
/// writes visual and numerical comparison results.  Registration deliberately uses
/// low-frequency luminance, so unrelated random speckles do not drive the alignment.
/// </summary>
public static class NakajimaRealImageComparison
{
    private const int RegistrationSize = 96;

    public static void Run(vis_3D renderer, string requestedFile,
        string requestedFov = "auto", bool useOtherCamera = false)
    {
        if (renderer == null)
        {
            ShowError("Renderer 'sphere' wurde nicht gefunden.");
            return;
        }

        string fileName = string.IsNullOrWhiteSpace(requestedFile) ? "image-000000.png" : requestedFile.Trim();
        if (!Path.HasExtension(fileName))
            fileName += ".png";
        string realPath = ResolveRealImagePath(fileName);
        if (!File.Exists(realPath))
        {
            ShowError("Realbild nicht gefunden:\n" + realPath);
            return;
        }

        Texture2D rendered = null;
        Texture2D real = null;
        Texture2D registered = null;
        Texture2D matched = null;
        Texture2D overlay = null;
        Texture2D difference = null;
        Texture2D experimentalTexturedRender = null;
        try
        {
            real = LoadTexture(realPath);
            //21092026 Nutzerwunsch: durchgehend quadratisch rendern (wie die TV-Pipeline).
            //Das Realfoto (2400x1728) wird dafuer mittig auf ein Quadrat beschnitten; die
            //Probe liegt in der Bildmitte und bleibt vollstaendig enthalten.
            real = CenterCropSquare(real);
            float comparisonAspect = 1f;
            float comparisonFov;
            bool parsedFov = float.TryParse(requestedFov, NumberStyles.Float,
                CultureInfo.InvariantCulture, out comparisonFov)
                || float.TryParse(requestedFov, NumberStyles.Float,
                    CultureInfo.CurrentCulture, out comparisonFov);
            Registration transform;
            string fovSearchReport = "";
            if (parsedFov)
            {
                comparisonFov = Mathf.Clamp(comparisonFov, 2f, 120f);
                rendered = renderer.capture_nakajima_comparison_image(comparisonFov, comparisonAspect,
                    useCam1: useOtherCamera);
                transform = FindRegistration(rendered, real);
            }
            else
            {
                // The experimental camera observes the narrow central measuring
                // band; the wide 70/90 degree views included the complete specimen.
                // Search only tighter perspective views so the simulated image has
                // the same useful framing as the photograph.
                float[] candidates = { 5f, 7.5f, 10f, 12.5f, 15f };
                comparisonFov = candidates[0];
                transform = new Registration { Score = float.NegativeInfinity };
                float bestSelectionScore = float.NegativeInfinity;
                var scores = new StringBuilder("Automatische FOV-Suche:");
                foreach (float candidateFov in candidates)
                {
                    Texture2D candidateImage = renderer.capture_nakajima_comparison_image(candidateFov,
                        comparisonAspect, useCam1: useOtherCamera);
                    Registration candidateTransform = FindRegistration(candidateImage, real);
                    // Registration may compensate a wrong camera zoom by stretching
                    // the photograph. Penalise that compensation when selecting FOV.
                    float selectionScore = candidateTransform.Score
                        - 0.35f * (Mathf.Abs(Mathf.Log(candidateTransform.ScaleX))
                            + Mathf.Abs(Mathf.Log(candidateTransform.ScaleY)))
                        - 0.10f * (Mathf.Abs(candidateTransform.OffsetX)
                            + Mathf.Abs(candidateTransform.OffsetY));
                    scores.Append("\n  ").Append(F(candidateFov)).Append("°: ")
                        .Append(F(selectionScore)).Append(" (Korrelation ")
                        .Append(F(candidateTransform.Score)).Append(")");
                    if (selectionScore > bestSelectionScore)
                    {
                        DestroyTexture(rendered);
                        rendered = candidateImage;
                        comparisonFov = candidateFov;
                        transform = candidateTransform;
                        bestSelectionScore = selectionScore;
                    }
                    else
                    {
                        DestroyTexture(candidateImage);
                    }
                }
                fovSearchReport = scores.ToString();
            }

            if (rendered == null || rendered.width < 2)
                throw new InvalidOperationException("Die Renderkamera liefert kein Bild.");

            //18092026 Die feste Offset-Korrektur (-0.033 / +0.055) wurde fuer die
            //cam_0-Aufnahmegeometrie ermittelt und passt nicht zwangslaeufig zur um den
            //gleichen Winkel entgegengesetzt geneigten cam_1. Deshalb nur fuer die
            //Standardkamera anwenden.
            if (IsCam00Image(realPath) && !useOtherCamera)
            {
                // The cam00 camera framing places the specimen slightly above and
                // to the right of the synthetic optical axis. Correct that known
                // acquisition offset after structural registration. Also preserve
                // the camera's physical aspect ratio: independent x/y fitting had
                // compressed the real specimen horizontally. Any excess now crops
                // naturally at the fixed output boundary.
                transform.ScaleX = transform.ScaleY;
                transform.OffsetX = Mathf.Clamp(transform.OffsetX - 0.033f, -0.30f, 0.30f);
                transform.OffsetY = Mathf.Clamp(transform.OffsetY + 0.055f, -0.30f, 0.30f);
                transform.Score = EvaluateRegistrationScore(rendered, real, transform);
                experimentalTexturedRender = renderer.capture_nakajima_comparison_image(
                    comparisonFov, comparisonAspect, real, transform.ScaleX,
                    transform.ScaleY, transform.OffsetX, transform.OffsetY, useCam1: useOtherCamera);
            }

            registered = WarpRealToRendered(real, rendered.width, rendered.height, transform);

            //18092026 Auskommentiert auf Nutzerwunsch: das hier ist der "fake Schatten" -
            //es faerbt den Render nachtraeglich zeilenweise dunkler/heller, und zwar
            //genau nach dem Helligkeitsprofil, das vorher aus dem ECHTEN Foto (real/registered)
            //extrahiert wurde. Der Schatten kam also nie aus echter 3D-Beleuchtung, sondern
            //wurde 1:1 vom Realbild auf den synthetischen Render kopiert. Erstmal deaktiviert,
            //damit als naechstes ein echter Schattenwurf in der 3D-Szene (Licht/Shadows) simuliert
            //werden kann, statt ihn hier reinzukuenstel.
            //if (IsCam00Image(realPath))
            //{
            //    Texture2D illuminatedRender = ApplyVerticalIlluminationProfile(rendered, registered);
            //    DestroyTexture(rendered);
            //    rendered = illuminatedRender;
            //}

            Statistics renderedStats = Measure(rendered, 0.12f);
            Statistics realStats = Measure(registered, 0.12f);
            float gain = renderedStats.Std > 1e-6f ? realStats.Std / renderedStats.Std : 1f;
            float offset = realStats.Mean - gain * renderedStats.Mean;
            matched = ApplyLinearMatch(rendered, gain, offset);

            Metrics metrics = CalculateMetrics(matched, registered);
            Statistics renderedSpeckle = Measure(matched, 0.30f);
            Statistics realSpeckle = Measure(registered, 0.30f);
            overlay = MakeOverlay(matched, registered);
            difference = MakeDifference(matched, registered, metrics.Mae);

            string outputDirectory = OutputDirectory;
            Directory.CreateDirectory(outputDirectory);
            //18092026 Nutzerwunsch: Vergleich mit der jeweils anderen (entgegengesetzt
            //geneigten) Kamera moeglich. Eigenes Datei-Praefix, damit die Standard-
            //Vergleichsbilder (cam_0) dabei nicht ueberschrieben werden.
            string filePrefix = useOtherCamera ? "cam1_" : "";
            string renderedPath = Save(rendered, outputDirectory, filePrefix + "01_render_aktuell.png");
            string experimentalTexturedPath = experimentalTexturedRender != null
                ? Save(experimentalTexturedRender, outputDirectory,
                    filePrefix + "01b_render_mit_realtextur.png") : null;
            string registeredPath = Save(registered, outputDirectory, filePrefix + "02_real_registriert.png");
            string matchedPath = Save(matched, outputDirectory, filePrefix + "03_render_an_realbild_angepasst.png");
            string overlayPath = Save(overlay, outputDirectory, filePrefix + "04_ueberlagerung.png");
            string differencePath = Save(difference, outputDirectory, filePrefix + "05_differenz.png");

            string report = BuildReport(fileName, comparisonFov, fovSearchReport, transform,
                metrics, renderedSpeckle, realSpeckle, gain, offset);
            File.WriteAllText(Path.Combine(outputDirectory, filePrefix + "vergleich.txt"), report, Encoding.UTF8);
            WriteTsv(Path.Combine(outputDirectory, filePrefix + "vergleich.tsv"), fileName, comparisonFov,
                transform, metrics, renderedSpeckle, realSpeckle, gain, offset);

            ExperimentImageGallery.ClearCurrentExperiment();
            ExperimentImageGallery.SetResultsText(report);
            ExperimentImageGallery.AddRenderedImage(renderedPath);
            ExperimentImageGallery.AddRenderedImage(experimentalTexturedPath);
            ExperimentImageGallery.AddRenderedImage(registeredPath);
            ExperimentImageGallery.AddRenderedImage(matchedPath);
            ExperimentImageGallery.AddRenderedImage(overlayPath);
            ExperimentImageGallery.AddRenderedImage(differencePath);
            ExperimentImageGallery.ShowWhenFinished();
            Debug.Log("Nakajima real-image comparison written to " + outputDirectory);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            ShowError("Realbild-Vergleich fehlgeschlagen:\n" + exception.Message);
        }
        finally
        {
            DestroyTexture(rendered);
            DestroyTexture(real);
            DestroyTexture(registered);
            DestroyTexture(matched);
            DestroyTexture(overlay);
            DestroyTexture(difference);
            DestroyTexture(experimentalTexturedRender);
        }
    }

    public static void LoadPrevious()
    {
        string reportPath = Path.Combine(OutputDirectory, "vergleich.txt");
        if (!File.Exists(reportPath))
        {
            ShowError("Es wurde noch kein Realbild-Vergleich gespeichert.");
            return;
        }

        ExperimentImageGallery.ClearCurrentExperiment();
        ExperimentImageGallery.SetResultsText(File.ReadAllText(reportPath));
        string[] files = Directory.GetFiles(OutputDirectory, "*.png", SearchOption.TopDirectoryOnly);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        foreach (string file in files)
            ExperimentImageGallery.AddRenderedImage(file);
        ExperimentImageGallery.ShowWhenFinished();
    }

    private static string OutputDirectory
    {
        get { return Path.Combine(Application.dataPath, "analysis_results", "nakajima_real_comparison"); }
    }

    private static string ResolveRealImagePath(string fileName)
    {
        // New Nakajima recordings live in cam00. Keep the previous folder as a
        // fallback so existing saved comparisons and manually entered names work.
        string[] folders = { "cam00", "nakajima_full_angle" };
        foreach (string folder in folders)
        {
            string candidate = Path.Combine(Application.dataPath, folder, fileName);
            if (File.Exists(candidate))
                return candidate;
        }
        return Path.Combine(Application.dataPath, folders[0], fileName);
    }

    private static bool IsCam00Image(string path)
    {
        string parent = Path.GetFileName(Path.GetDirectoryName(path));
        return string.Equals(parent, "cam00", StringComparison.OrdinalIgnoreCase);
    }

    private static float EvaluateRegistrationScore(Texture2D rendered, Texture2D real,
        Registration transform)
    {
        float[] synthetic = BoxBlur(Downsample(rendered, RegistrationSize,
            RegistrationSize), RegistrationSize, 2);
        float[] photograph = BoxBlur(Downsample(real, RegistrationSize,
            RegistrationSize), RegistrationSize, 2);
        return Score(synthetic, photograph, RegistrationSize, transform);
    }

    //21092026 mittiger quadratischer Ausschnitt (Kantenlaenge = kleinere Seite); gibt bei
    //bereits quadratischer Textur dieselbe zurueck, sonst eine neue (Original wird zerstoert).
    private static Texture2D CenterCropSquare(Texture2D source)
    {
        if (source == null || source.width == source.height)
            return source;
        int side = Mathf.Min(source.width, source.height);
        int x0 = (source.width - side) / 2;
        int y0 = (source.height - side) / 2;
        Texture2D cropped = new Texture2D(side, side, TextureFormat.RGBA32, false);
        cropped.SetPixels(source.GetPixels(x0, y0, side, side));
        cropped.Apply();
        cropped.wrapMode = TextureWrapMode.Clamp;
        UnityEngine.Object.Destroy(source);
        return cropped;
    }

    private static Texture2D LoadTexture(string path)
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(File.ReadAllBytes(path), false))
            throw new IOException("Bild konnte nicht gelesen werden: " + path);
        return texture;
    }

    private static Texture2D ApplyVerticalIlluminationProfile(Texture2D rendered,
        Texture2D registeredReal)
    {
        int width = rendered.width;
        int height = rendered.height;
        Color[] source = rendered.GetPixels();
        Color[] real = registeredReal.GetPixels();
        float[] profile = new float[height];

        // Average only across the central specimen band, excluding dies and
        // cropped borders. This isolates the broad illumination/shadow field.
        int x0 = Mathf.RoundToInt(width * 0.35f);
        int x1 = Mathf.RoundToInt(width * 0.65f);
        for (int y = 0; y < height; y++)
        {
            double sum = 0.0;
            for (int x = x0; x < x1; x++)
                sum += Luminance(real[y * width + x]);
            profile[y] = (float)(sum / Mathf.Max(1, x1 - x0));
        }

        int radius = Mathf.Max(8, height / 12);
        float[] smooth = new float[height];
        for (int y = 0; y < height; y++)
        {
            int from = Mathf.Max(0, y - radius);
            int to = Mathf.Min(height - 1, y + radius);
            double sum = 0.0;
            for (int j = from; j <= to; j++)
                sum += profile[j];
            smooth[y] = (float)(sum / (to - from + 1));
        }

        int centre0 = Mathf.RoundToInt(height * 0.38f);
        int centre1 = Mathf.RoundToInt(height * 0.62f);
        double centreSum = 0.0;
        for (int y = centre0; y < centre1; y++)
            centreSum += smooth[y];
        float reference = (float)(centreSum / Mathf.Max(1, centre1 - centre0));
        reference = Mathf.Max(reference, 1e-4f);

        Color[] output = new Color[source.Length];
        for (int y = 0; y < height; y++)
        {
            float factor = Mathf.Clamp(smooth[y] / reference, 0.55f, 1.15f);
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                Color c = source[index];
                output[index] = new Color(c.r * factor, c.g * factor,
                    c.b * factor, c.a);
            }
        }
        return MakeTexture(width, height, output);
    }

    private static Registration FindRegistration(Texture2D rendered, Texture2D real)
    {
        float[] synthetic = Downsample(rendered, RegistrationSize, RegistrationSize);
        float[] photograph = Downsample(real, RegistrationSize, RegistrationSize);
        synthetic = BoxBlur(synthetic, RegistrationSize, 2);
        photograph = BoxBlur(photograph, RegistrationSize, 2);

        Registration best = new Registration { ScaleX = 1f, ScaleY = 1f, OffsetX = 0f, OffsetY = 0f };
        float bestScore = Score(synthetic, photograph, RegistrationSize, best);
        float[] scaleSteps = { 0.16f, 0.055f, 0.018f };
        float[] offsetSteps = { 0.10f, 0.032f, 0.010f };
        for (int level = 0; level < scaleSteps.Length; level++)
        {
            Registration centre = best;
            for (int ix = -1; ix <= 1; ix++)
            for (int iy = -1; iy <= 1; iy++)
            for (int itx = -1; itx <= 1; itx++)
            for (int ity = -1; ity <= 1; ity++)
            {
                Registration candidate = new Registration
                {
                    ScaleX = Mathf.Clamp(centre.ScaleX + ix * scaleSteps[level], 0.50f, 1.60f),
                    ScaleY = Mathf.Clamp(centre.ScaleY + iy * scaleSteps[level], 0.50f, 1.60f),
                    OffsetX = Mathf.Clamp(centre.OffsetX + itx * offsetSteps[level], -0.30f, 0.30f),
                    OffsetY = Mathf.Clamp(centre.OffsetY + ity * offsetSteps[level], -0.30f, 0.30f)
                };
                float score = Score(synthetic, photograph, RegistrationSize, candidate);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
        }
        best.Score = bestScore;
        return best;
    }

    private static float Score(float[] target, float[] source, int size, Registration t)
    {
        double sumA = 0, sumB = 0, sumAA = 0, sumBB = 0, sumAB = 0;
        int count = 0;
        for (int y = 8; y < size - 8; y++)
        for (int x = 8; x < size - 8; x++)
        {
            float u = (x + 0.5f) / size;
            float v = (y + 0.5f) / size;
            float su = 0.5f + (u - 0.5f) * t.ScaleX + t.OffsetX;
            float sv = 0.5f + (v - 0.5f) * t.ScaleY + t.OffsetY;
            if (su < 0f || su > 1f || sv < 0f || sv > 1f)
                continue;
            float a = target[y * size + x];
            float b = Sample(source, size, size, su, sv);
            sumA += a; sumB += b; sumAA += a * a; sumBB += b * b; sumAB += a * b;
            count++;
        }
        if (count < 100)
            return -1f;
        double covariance = sumAB - sumA * sumB / count;
        double varianceA = sumAA - sumA * sumA / count;
        double varianceB = sumBB - sumB * sumB / count;
        return (float)(covariance / Math.Sqrt(Math.Max(1e-12, varianceA * varianceB)));
    }

    private static Texture2D WarpRealToRendered(Texture2D real, int width, int height, Registration t)
    {
        Color[] source = real.GetPixels();
        Color[] output = new Color[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float u = (x + 0.5f) / width;
            float v = (y + 0.5f) / height;
            float su = 0.5f + (u - 0.5f) * t.ScaleX + t.OffsetX;
            float sv = 0.5f + (v - 0.5f) * t.ScaleY + t.OffsetY;
            float px = su * real.width;
            float py = sv * real.height;
            output[y * width + x] = SampleColor(source, real.width, real.height, px, py);
        }
        return MakeTexture(width, height, output);
    }

    private static float[] Downsample(Texture2D texture, int width, int height)
    {
        Color[] pixels = texture.GetPixels();
        float[] result = new float[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float px = (x + 0.5f) * texture.width / width;
            float py = (y + 0.5f) * texture.height / height;
            result[y * width + x] = Luminance(SampleColor(pixels, texture.width, texture.height, px, py));
        }
        return result;
    }

    private static float[] BoxBlur(float[] input, int size, int radius)
    {
        float[] output = new float[input.Length];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float sum = 0f;
            int count = 0;
            for (int yy = Mathf.Max(0, y - radius); yy <= Mathf.Min(size - 1, y + radius); yy++)
            for (int xx = Mathf.Max(0, x - radius); xx <= Mathf.Min(size - 1, x + radius); xx++)
            {
                sum += input[yy * size + xx];
                count++;
            }
            output[y * size + x] = sum / count;
        }
        return output;
    }

    private static Texture2D ApplyLinearMatch(Texture2D input, float gain, float offset)
    {
        Color[] pixels = input.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            float value = Mathf.Clamp01(Luminance(pixels[i]) * gain + offset);
            pixels[i] = new Color(value, value, value, 1f);
        }
        return MakeTexture(input.width, input.height, pixels);
    }

    private static Metrics CalculateMetrics(Texture2D a, Texture2D b)
    {
        float[] aa = Grayscale(a);
        float[] bb = Grayscale(b);
        double sumA = 0, sumB = 0, sumAA = 0, sumBB = 0, sumAB = 0, sumAbs = 0, sumSquared = 0;
        int count = aa.Length;
        for (int i = 0; i < count; i++)
        {
            sumA += aa[i]; sumB += bb[i]; sumAA += aa[i] * aa[i]; sumBB += bb[i] * bb[i];
            sumAB += aa[i] * bb[i];
            double error = aa[i] - bb[i];
            sumAbs += Math.Abs(error); sumSquared += error * error;
        }
        double meanA = sumA / count, meanB = sumB / count;
        double varA = sumAA / count - meanA * meanA;
        double varB = sumBB / count - meanB * meanB;
        double covariance = sumAB / count - meanA * meanB;
        const double c1 = 0.0001, c2 = 0.0009;
        return new Metrics
        {
            Correlation = (float)(covariance / Math.Sqrt(Math.Max(1e-12, varA * varB))),
            Ssim = (float)(((2 * meanA * meanB + c1) * (2 * covariance + c2)) /
                           ((meanA * meanA + meanB * meanB + c1) * (varA + varB + c2))),
            Mae = (float)(sumAbs / count),
            Rmse = (float)Math.Sqrt(sumSquared / count)
        };
    }

    private static Statistics Measure(Texture2D texture, float margin)
    {
        Color[] pixels = texture.GetPixels();
        int x0 = Mathf.RoundToInt(texture.width * margin);
        int x1 = texture.width - x0;
        int y0 = Mathf.RoundToInt(texture.height * margin);
        int y1 = texture.height - y0;
        double sum = 0, sum2 = 0;
        int count = 0;
        for (int y = y0; y < y1; y++)
        for (int x = x0; x < x1; x++)
        {
            float value = Luminance(pixels[y * texture.width + x]);
            sum += value; sum2 += value * value; count++;
        }
        float mean = (float)(sum / Math.Max(1, count));
        float variance = (float)(sum2 / Math.Max(1, count) - mean * mean);
        return new Statistics { Mean = mean, Std = Mathf.Sqrt(Mathf.Max(0f, variance)) };
    }

    private static Texture2D MakeOverlay(Texture2D rendered, Texture2D real)
    {
        Color[] a = rendered.GetPixels();
        Color[] b = real.GetPixels();
        Color[] result = new Color[a.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = new Color(Luminance(a[i]), Luminance(b[i]), Luminance(b[i]), 1f);
        return MakeTexture(rendered.width, rendered.height, result);
    }

    private static Texture2D MakeDifference(Texture2D rendered, Texture2D real, float meanError)
    {
        Color[] a = rendered.GetPixels();
        Color[] b = real.GetPixels();
        Color[] result = new Color[a.Length];
        float scale = 1f / Mathf.Max(0.04f, meanError * 3f);
        for (int i = 0; i < result.Length; i++)
        {
            float d = Mathf.Clamp01(Mathf.Abs(Luminance(a[i]) - Luminance(b[i])) * scale);
            result[i] = new Color(d, Mathf.Clamp01(1.5f * d * (1f - d)), 1f - d, 1f);
        }
        return MakeTexture(rendered.width, rendered.height, result);
    }

    private static string BuildReport(string fileName, float comparisonFov, string fovSearchReport,
        Registration t, Metrics m,
        Statistics rendered, Statistics real, float gain, float offset)
    {
        return "Nakajima-Realbild: " + fileName
            + "\nKamera-FOV: " + F(comparisonFov) + "°"
            + (string.IsNullOrEmpty(fovSearchReport) ? "" : "\n\n" + fovSearchReport)
            + "\n\nRegistrierung (Real → Render)"
            + "\n  Struktur-Korrelation: " + F(t.Score)
            + "\n  Skalierung x/y: " + F(t.ScaleX) + " / " + F(t.ScaleY)
            + "\n  Verschiebung x/y: " + F(t.OffsetX) + " / " + F(t.OffsetY)
            + "\n\nBildvergleich nach Render-Helligkeitsabgleich"
            + "\n  Korrelation: " + F(m.Correlation)
            + "\n  SSIM: " + F(m.Ssim)
            + "\n  MAE: " + F(m.Mae)
            + "\n  RMSE: " + F(m.Rmse)
            + "\n\nSpeckle-Bereich (zentrale 40 %)"
            + "\n  Render μ/σ: " + F(rendered.Mean) + " / " + F(rendered.Std)
            + "\n  Real μ/σ: " + F(real.Mean) + " / " + F(real.Std)
            + "\n  Kontrast σ/μ: " + F(rendered.Contrast) + " / " + F(real.Contrast)
            + "\n\nPhotometrische Anpassung Render:"
            + "\n  I' = " + F(gain) + " · I + " + F(offset)
            + "\n\nSSIM/Korrelation: 1 ist ideal. MAE/RMSE: 0 ist ideal.";
    }

    private static void WriteTsv(string path, string fileName, float comparisonFov,
        Registration t, Metrics m,
        Statistics rendered, Statistics real, float gain, float offset)
    {
        string header = "real_image\tcamera_fov_deg\tregistration_correlation\tscale_x\tscale_y\toffset_x\toffset_y"
            + "\tcorrelation\tssim\tmae\trmse\trender_mean\trender_std\treal_mean\treal_std"
            + "\trender_contrast\treal_contrast\tphotometric_gain\tphotometric_offset\n";
        string row = fileName + "\t" + F(comparisonFov) + "\t" + F(t.Score)
            + "\t" + F(t.ScaleX) + "\t" + F(t.ScaleY)
            + "\t" + F(t.OffsetX) + "\t" + F(t.OffsetY) + "\t" + F(m.Correlation)
            + "\t" + F(m.Ssim) + "\t" + F(m.Mae) + "\t" + F(m.Rmse)
            + "\t" + F(rendered.Mean) + "\t" + F(rendered.Std) + "\t" + F(real.Mean)
            + "\t" + F(real.Std) + "\t" + F(rendered.Contrast) + "\t" + F(real.Contrast)
            + "\t" + F(gain) + "\t" + F(offset) + "\n";
        File.WriteAllText(path, header + row, Encoding.UTF8);
    }

    private static string Save(Texture2D texture, string directory, string name)
    {
        string path = Path.Combine(directory, name);
        File.WriteAllBytes(path, texture.EncodeToPNG());
        return path;
    }

    private static Texture2D MakeTexture(int width, int height, Color[] pixels)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.SetPixels(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static float[] Grayscale(Texture2D texture)
    {
        Color[] pixels = texture.GetPixels();
        float[] result = new float[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
            result[i] = Luminance(pixels[i]);
        return result;
    }

    private static float Sample(float[] values, int width, int height, float u, float v)
    {
        float x = Mathf.Clamp01(u) * (width - 1);
        float y = Mathf.Clamp01(v) * (height - 1);
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        int x1 = Mathf.Min(width - 1, x0 + 1), y1 = Mathf.Min(height - 1, y0 + 1);
        float fx = x - x0, fy = y - y0;
        return Mathf.Lerp(Mathf.Lerp(values[y0 * width + x0], values[y0 * width + x1], fx),
            Mathf.Lerp(values[y1 * width + x0], values[y1 * width + x1], fx), fy);
    }

    private static Color SampleColor(Color[] values, int width, int height, float x, float y)
    {
        x = Mathf.Clamp(x, 0f, width - 1f);
        y = Mathf.Clamp(y, 0f, height - 1f);
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        int x1 = Mathf.Min(width - 1, x0 + 1), y1 = Mathf.Min(height - 1, y0 + 1);
        float fx = x - x0, fy = y - y0;
        return Color.Lerp(Color.Lerp(values[y0 * width + x0], values[y0 * width + x1], fx),
            Color.Lerp(values[y1 * width + x0], values[y1 * width + x1], fx), fy);
    }

    private static float Luminance(Color c)
    {
        return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
    }

    private static string F(float value)
    {
        return value.ToString("G6", CultureInfo.InvariantCulture);
    }

    private static void ShowError(string message)
    {
        ExperimentImageGallery.SetResultsText(message);
        ExperimentImageGallery.ShowResultsWindow();
        Debug.LogWarning(message);
    }

    private static void DestroyTexture(Texture2D texture)
    {
        if (texture != null)
            UnityEngine.Object.Destroy(texture);
    }

    private struct Registration
    {
        public float ScaleX, ScaleY, OffsetX, OffsetY, Score;
    }

    private struct Metrics
    {
        public float Correlation, Ssim, Mae, Rmse;
    }

    private struct Statistics
    {
        public float Mean, Std;
        public float Contrast { get { return Mean > 1e-6f ? Std / Mean : 0f; } }
    }
}
