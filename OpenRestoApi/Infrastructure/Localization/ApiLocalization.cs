using System.Text.RegularExpressions;

namespace OpenRestoApi.Infrastructure.Localization;

public static partial class ApiLocalization
{
    public const string English = "en";
    public const string ColombianSpanish = "es-CO";

    public static string ResolveLocale(string? acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
            return English;

        string token = acceptLanguage
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault()?
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault()?
            .Trim()
            .Replace('_', '-') ?? English;

        return token.StartsWith("es", StringComparison.OrdinalIgnoreCase)
            ? ColombianSpanish
            : English;
    }

    public static string Localize(HttpContext? httpContext, string message)
        => Localize(ResolveLocale(httpContext?.Request.Headers.AcceptLanguage.ToString()), message);

    public static string Localize(string locale, string message)
    {
        if (!string.Equals(locale, ColombianSpanish, StringComparison.OrdinalIgnoreCase))
            return message;

        if (StaticTranslations.TryGetValue(message, out string? translated))
            return translated;

        Match seatMatch = SeatsPattern().Match(message);
        if (seatMatch.Success)
        {
            return $"Esta mesa solo tiene {seatMatch.Groups[1].Value} puestos, pero se solicitaron {seatMatch.Groups[2].Value} comensales.";
        }

        Match emailSentMatch = EmailSentPattern().Match(message);
        if (emailSentMatch.Success)
        {
            return $"Correo enviado a {emailSentMatch.Groups[1].Value}.";
        }

        Match failedSendMatch = FailedToSendPattern().Match(message);
        if (failedSendMatch.Success)
        {
            return $"No se pudo enviar: {failedSendMatch.Groups[1].Value}";
        }

        Match connectionFailedMatch = ConnectionFailedPattern().Match(message);
        if (connectionFailedMatch.Success)
        {
            return $"No se pudo conectar: {connectionFailedMatch.Groups[1].Value}";
        }

        Match oversizedTableMatch = OversizedTablePattern().Match(message);
        if (oversizedTableMatch.Success)
        {
            return $"Esta mesa tiene {oversizedTableMatch.Groups[1].Value} puestos, lo que es demasiado para un grupo de {oversizedTableMatch.Groups[2].Value}.";
        }

        Match unexpectedDetailedMatch = UnexpectedDetailedPattern().Match(message);
        if (unexpectedDetailedMatch.Success)
        {
            return $"Ocurrio un error inesperado: {unexpectedDetailedMatch.Groups[1].Value}";
        }

        return message;
    }

    private static readonly IReadOnlyDictionary<string, string> StaticTranslations =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Invalid email or password."] = "Correo o contrasena invalidos.",
            ["Login successful."] = "Inicio de sesion exitoso.",
            ["Logged out."] = "Sesion cerrada.",
            ["Current password is incorrect."] = "La contrasena actual es incorrecta.",
            ["Password changed successfully."] = "Contrasena actualizada correctamente.",
            ["Email changed successfully."] = "Correo actualizado correctamente.",
            ["Question and answer are required."] = "La pregunta y la respuesta son obligatorias.",
            ["Security question configured."] = "Pregunta de seguridad configurada.",
            ["Security question not configured for this account."] = "No hay una pregunta de seguridad configurada para esta cuenta.",
            ["Incorrect answer."] = "Respuesta incorrecta.",
            ["Invalid or expired reset token."] = "El token de restablecimiento no es valido o vencio.",
            ["Password reset successfully."] = "Contrasena restablecida correctamente.",
            ["Password must be at least 6 characters."] = "La contrasena debe tener al menos 6 caracteres.",
            ["A valid email address is required."] = "Se requiere un correo electronico valido.",
            ["New email must be different from the current email."] = "El nuevo correo debe ser diferente al correo actual.",
            ["An account with that email already exists."] = "Ya existe una cuenta con ese correo.",
            ["Invalid role. Allowed values: SuperAdmin, BookingViewer, BookingEditor."] = "Rol no valido. Valores permitidos: SuperAdmin, BookingViewer, BookingEditor.",
            ["Email is required to look up a booking."] = "Se requiere un correo para consultar una reserva.",
            ["No booking found matching that reference and email."] = "No se encontro ninguna reserva con esa referencia y correo.",
            ["Email is required to cancel a booking."] = "Se requiere un correo para cancelar una reserva.",
            ["Restaurant not found."] = "No se encontro el restaurante.",
            ["Bookings for this restaurant are currently paused. Please try again later."] = "Las reservas de este restaurante estan en pausa en este momento. Intentalo mas tarde.",
            ["Cannot create a booking in the past."] = "No se puede crear una reserva en el pasado.",
            ["Cannot hold a table for a past time."] = "No se puede retener una mesa para una hora pasada.",
            ["Bookings are currently paused for this restaurant."] = "Las reservas estan en pausa para este restaurante en este momento.",
            ["Specify both TableId and SectionId, or neither for auto-assign."] = "Especifique ambos, TableId y SectionId, o ninguno para la asignacion automatica.",
            ["This table is already booked for that time."] = "Esta mesa ya esta reservada para ese horario.",
            ["This table is currently being held by another user. Please try again shortly."] = "Esta mesa esta retenida por otro usuario en este momento. Intentalo de nuevo pronto.",
            ["No tables are available for the requested time and party size."] = "No hay mesas disponibles para la hora y el tamano del grupo solicitados.",
            ["All suitable tables are currently being held by other users. Please try again shortly."] = "Todas las mesas adecuadas estan retenidas por otros usuarios en este momento. Intentalo de nuevo pronto.",
            ["Cannot cancel a booking that has already passed."] = "No se puede cancelar una reserva que ya paso.",
            ["This location accepts walk-ins only and does not take online bookings."] = "Este local solo acepta clientes sin reserva y no recibe reservas en linea.",
            ["This location accepts walk-ins only on the selected day. Please choose another day or just come in."] = "Este local solo acepta clientes sin reserva en el dia seleccionado. Elija otro dia o venga directamente.",
            ["This location accepts walk-ins only on the selected day."] = "Este local solo acepta clientes sin reserva en el dia seleccionado.",
            ["The restaurant is closed at the requested time."] = "El restaurante esta cerrado a la hora solicitada.",
            ["Specify both TableId and SectionId, or omit both for auto-assign."] = "Especifique ambos, TableId y SectionId, o omita ambos para la asignacion automatica.",
            ["This table is already held by another user. Please select a different table or try again shortly."] = "Esta mesa ya esta retenida por otro usuario. Selecciona otra mesa o intentalo de nuevo pronto.",
            ["Seats is required for auto-assign so the server can pick a table that fits your party."] = "Se requiere Seats para la asignacion automatica para que el servidor pueda elegir una mesa adecuada para su grupo.",
            ["Name is required."] = "El nombre es obligatorio.",
            ["Bookings paused successfully."] = "Las reservas se pausaron correctamente.",
            ["Bookings unpaused successfully."] = "Las reservas se reanudaron correctamente.",
            ["Bookings extended successfully."] = "Las reservas se extendieron correctamente.",
            ["sectionIds must include exactly the restaurant's current sections, with no duplicates."] = "sectionIds debe incluir exactamente las secciones actuales del restaurante, sin duplicados.",
            ["Restaurant not found or has no sections."] = "No se encontro el restaurante o no tiene secciones.",
            ["Subject and body are required."] = "El asunto y el cuerpo son obligatorios.",
            ["Customer email is not available."] = "El correo del cliente no esta disponible.",
            ["Booking restored successfully."] = "Reserva restaurada correctamente.",
            ["Table not found in the specified section."] = "No se encontro la mesa en la seccion especificada.",
            ["Section does not belong to this restaurant."] = "La seccion no pertenece a este restaurante.",
            ["This table already has a booking that overlaps with the requested time."] = "Esta mesa ya tiene una reserva que se cruza con el horario solicitado.",
            ["Invalid restaurant."] = "Restaurante invalido.",
            ["Invalid table for this restaurant."] = "Mesa invalida para este restaurante.",
            ["Provide tableId when reassigning to a different section."] = "Proporcione tableId al reasignar a una seccion diferente.",
            ["This update would cause a conflict with an existing booking."] = "Esta actualizacion causaria un conflicto con una reserva existente.",
            ["Booking is already active."] = "La reserva ya esta activa.",
            ["Email is not configured."] = "El correo no esta configurado.",
            ["Admin:Password must be configured before first use. Set it via ADMIN_PASSWORD env var."] = "Admin:Password debe configurarse antes del primer uso. Definalo con la variable de entorno ADMIN_PASSWORD.",
            ["An unexpected error occurred."] = "Ocurrio un error inesperado.",
            ["restaurantId is required."] = "restaurantId es obligatorio.",
            ["List of notification IDs is required."] = "La lista de IDs de notificaciones es obligatoria.",
            ["Brand settings saved."] = "La configuracion de marca se guardo correctamente.",
            ["Email settings saved."] = "La configuracion de correo se guardo correctamente.",
            ["Connection successful."] = "Conexion exitosa.",
            ["Only JPEG, PNG, and WebP images are accepted."] = "Solo se aceptan imagenes JPEG, PNG y WebP.",
            ["Hero image must be under 5 MB."] = "La imagen principal debe pesar menos de 5 MB.",
            ["Location image must be under 2 MB."] = "La imagen de la ubicacion debe pesar menos de 2 MB.",
            ["Only PDF menu files are accepted."] = "Solo se aceptan archivos PDF para el menu.",
            ["Menu file must be under 10 MB."] = "El archivo del menu debe pesar menos de 10 MB.",
        };

    [GeneratedRegex("^This table only has (\\d+) seats, but (\\d+) guests were requested\\.$")]
    private static partial Regex SeatsPattern();

    [GeneratedRegex("^This table has (\\d+) seats, which is too large for a party of (\\d+)\\.$")]
    private static partial Regex OversizedTablePattern();

    [GeneratedRegex("^Email sent to (.+)\\.$")]
    private static partial Regex EmailSentPattern();

    [GeneratedRegex("^Failed to send: (.+)$")]
    private static partial Regex FailedToSendPattern();

    [GeneratedRegex("^Connection failed: (.+)$")]
    private static partial Regex ConnectionFailedPattern();

    [GeneratedRegex("^An unexpected error occurred: (.+)$")]
    private static partial Regex UnexpectedDetailedPattern();
}
