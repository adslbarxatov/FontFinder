using Microsoft.Win32;
using System.Globalization;

namespace RD_AAOW
	{
	/// <summary>
	/// Поддерживаемые языки приложения
	/// </summary>
	public enum SupportedLanguages
		{
		/// <summary>
		/// Русский
		/// </summary>
		ru_ru,

		/// <summary>
		/// Английский (США)
		/// </summary>
		en_us
		}

	/// <summary>
	/// Класс обеспечивает доступ к языковым настройкам приложения
	/// </summary>
	public static class Localization
		{
		/// <summary>
		/// Название параметра, хранящего текущий язык интерфейса
		/// </summary>
		public const string LanguageValueName = "Language";

		/// <summary>
		/// Количество доступных языков интерфейса
		/// </summary>
		public const uint AvailableLanguages = 2;

		/// <summary>
		/// Возвращает или задаёт текущий язык интерфейса приложения
		/// </summary>
		public static SupportedLanguages CurrentLanguage
			{
			// Запрос
			get
				{
				// Получение значения
				string lang = GetCurrentLanguage ();

				// При пустом значении пробуем получить язык от системы
				if (lang == "")
					{
					CultureInfo ci = CultureInfo.CurrentCulture;

					switch (ci.ToString ().ToLower ())
						{
						case "ru-ru":
							return SupportedLanguages.ru_ru;
						}
					}

				// Определение
				switch (lang)
					{
					case "ru_ru":
						return SupportedLanguages.ru_ru;

					default:
						return SupportedLanguages.en_us;
					}
				}

			// Установка
			set
				{
				try
					{
					Registry.SetValue (ProgramDescription.AssemblySettingsKey,
						LanguageValueName, value.ToString ());
					}
				catch
					{
					}
				}
			}

		/// <summary>
		/// Возвращает факт предыдущей установки языка приложения
		/// </summary>
		public static bool IsCurrentLanguageSpecified
			{
			get
				{
				return (GetCurrentLanguage () != "");
				}
			}

		// Метод запрашивает настройку из реестра
		private static string GetCurrentLanguage ()
			{
			// Получение значения
			string lang = "";
			try
				{
				lang = Registry.GetValue (ProgramDescription.AssemblySettingsKey,
					LanguageValueName, "").ToString ();
				}
			catch
				{
				}

			return lang;
			}

		/// <summary>
		/// Метод возвращает локализованный текст по указанному идентификатору
		/// </summary>
		/// <param name="TextName">Идентификатор текстового фрагмента</param>
		/// <param name="Language">Требуемый язык локализации</param>
		/// <returns>Локализованный текстовый фрагмент</returns>
		public static string GetText (string TextName, SupportedLanguages Language)
			{
			switch (Language)
				{
				default:
					return ProgramDescription.AssemblyLocalizationRMs[0].GetString (TextName);

				case SupportedLanguages.ru_ru:
					return ProgramDescription.AssemblyLocalizationRMs[1].GetString (TextName);
				}
			}
		}
	}
