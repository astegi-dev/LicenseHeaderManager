namespace Core
{
  public enum ErrorType
  {
    /// <summary>
    /// Specifies that the license header definition file contains text that is not configured as comment for the file's language.
    /// </summary>
    NonCommentText,

    /// <summary>
    /// Specifies that no configuration could be found for the file's language.
    /// </summary>
    LanguageNotFound,

    /// <summary>
    /// Specifies that an error occured while parsing the license header that was found in the file.
    /// </summary>
    ParsingError,

    /// <summary>
    /// An unspecified error occured.
    /// </summary>
    Miscellaneous
  }
}