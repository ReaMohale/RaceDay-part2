namespace RaceDay.Api.Swagger;

/// <summary>
/// A light touch of RaceDay colour on the default Swagger UI: dark top bar with an orange
/// accent line, green headings and buttons, and slightly warmer method colours.
/// </summary>
public static class SwaggerTheme
{
    public const string HeadContent = """
        <style>
          .swagger-ui .topbar { background-color: #191b1d; border-bottom: 3px solid #ff5a36; }
          .swagger-ui .info .title { color: #3b6255; }
          .swagger-ui .info a { color: #3b6255; }
          .swagger-ui .scheme-container { background: #f5f2ea; box-shadow: none; border-bottom: 1px solid #d8d3c4; }
          .swagger-ui .opblock-tag { border-bottom: 1px solid #d8d3c4; }

          .swagger-ui .btn.execute { background-color: #3b6255; border-color: #3b6255; color: #fff; }
          .swagger-ui .btn.execute:hover { background-color: #2f4f44; }

          .swagger-ui .opblock.opblock-post { background: rgba(59, 98, 85, .07); border-color: #3b6255; }
          .swagger-ui .opblock.opblock-post .opblock-summary-method { background: #3b6255; }
          .swagger-ui .opblock.opblock-put { background: rgba(232, 178, 61, .08); border-color: #c98f1a; }
          .swagger-ui .opblock.opblock-put .opblock-summary-method { background: #c98f1a; }
          .swagger-ui .opblock.opblock-delete { background: rgba(255, 90, 54, .07); border-color: #e0482a; }
          .swagger-ui .opblock.opblock-delete .opblock-summary-method { background: #e0482a; }
        </style>
        """;
}
