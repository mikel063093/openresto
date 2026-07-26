using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Controllers;
using OpenRestoApi.Infrastructure.Cookies;

namespace OpenRestoApi.Infrastructure.OpenApi;

internal static class OpenApiExampleFactory
{
    public static JsonNode? CreateForType(Type type)
    {
        Type targetType = Nullable.GetUnderlyingType(type) ?? type;

        if (targetType == typeof(MessageResponse))
        {
            return JsonSerializer.SerializeToNode(new MessageResponse
            {
                Message = "The requested booking could not be completed."
            });
        }

        if (targetType == typeof(ProblemDetails))
        {
            return JsonSerializer.SerializeToNode(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                title = "Bad Request",
                status = 400,
                detail = "The request could not be processed.",
                instance = "/api/example"
            });
        }

        if (targetType == typeof(ValidationProblemDetails))
        {
            return JsonSerializer.SerializeToNode(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                title = "One or more validation errors occurred.",
                status = 400,
                instance = "/api/example",
                traceId = "00-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-bbbbbbbbbbbbbbbb-00",
                errors = new Dictionary<string, string[]>
                {
                    ["email"] = ["A valid email address is required."]
                }
            });
        }

        if (targetType == typeof(AuthIdentityResponse))
        {
            return JsonSerializer.SerializeToNode(new AuthIdentityResponse
            {
                Email = "admin@example.com",
                Role = nameof(AdminRole.SuperAdmin)
            });
        }

        if (targetType == typeof(EmailChangeResponse))
        {
            return JsonSerializer.SerializeToNode(new EmailChangeResponse
            {
                Message = "Email changed successfully.",
                Email = "admin.updated@example.com"
            });
        }

        if (targetType == typeof(PvqVerifyResponse))
        {
            return JsonSerializer.SerializeToNode(new PvqVerifyResponse
            {
                ResetToken = "pvq-reset-token-example"
            });
        }

        if (targetType == typeof(UrlResponse))
        {
            return JsonSerializer.SerializeToNode(new UrlResponse
            {
                Url = "/media/location-7.webp?v=1764518400000"
            });
        }

        if (targetType == typeof(BookingExtendResponse))
        {
            return JsonSerializer.SerializeToNode(new BookingExtendResponse
            {
                EndTime = DateTime.Parse("2026-07-26T19:30:00Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal)
            });
        }

        if (targetType == typeof(RestaurantExtendResponse))
        {
            return JsonSerializer.SerializeToNode(new RestaurantExtendResponse
            {
                Message = "Bookings extended successfully.",
                ExtendedBookings = [CreateBookingDetail()]
            });
        }

        if (targetType == typeof(NotificationListResponse))
        {
            return JsonSerializer.SerializeToNode(new NotificationListResponse
            {
                Items = [CreateNotification()],
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            });
        }

        if (targetType == typeof(CountResponse))
        {
            return JsonSerializer.SerializeToNode(new CountResponse { Count = 3 });
        }

        if (targetType == typeof(HealthResponse))
        {
            return JsonSerializer.SerializeToNode(new HealthResponse { Status = "ok" });
        }

        if (targetType == typeof(VapidPublicKeyResponse))
        {
            return JsonSerializer.SerializeToNode(new VapidPublicKeyResponse
            {
                PublicKey = "BOrnWqg7y5nZ_example_public_key"
            });
        }

        if (targetType == typeof(ErrorResponse))
        {
            return JsonSerializer.SerializeToNode(new ErrorResponse
            {
                Error = "restaurantId is required."
            });
        }

        if (targetType == typeof(BrandResponse))
        {
            return JsonSerializer.SerializeToNode(new BrandResponse
            {
                AppName = "Open Resto",
                PrimaryColor = "#0a7ea4",
                AccentColor = "#ff7a18",
                HeaderImageUrl = "/media/hero.webp?v=1764518400000",
                WebsiteUrl = "https://openresto.example",
                FaviconIcon = "utensils",
                CopyrightText = "Open Resto",
                Subtitle = "Modern table booking for independent restaurants.",
                HighlightsHeading = "Why guests book here",
                HighlightsSubheading = "Fast reservations, clear availability, no phone tag.",
                HeaderImageFit = "Cover"
            });
        }

        if (targetType == typeof(RestaurantDto))
        {
            return JsonSerializer.SerializeToNode(CreateRestaurant());
        }

        if (targetType == typeof(SectionDto))
        {
            return JsonSerializer.SerializeToNode(CreateSection());
        }

        if (targetType == typeof(TableDto))
        {
            return JsonSerializer.SerializeToNode(new TableDto
            {
                Id = 12,
                Name = "Patio 4",
                Seats = 4
            });
        }

        if (targetType == typeof(BookingDto))
        {
            return JsonSerializer.SerializeToNode(CreateBooking());
        }

        if (targetType == typeof(BookingDetailDto))
        {
            return JsonSerializer.SerializeToNode(CreateBookingDetail());
        }

        if (targetType == typeof(HoldResponse))
        {
            return JsonSerializer.SerializeToNode(new HoldResponse
            {
                HoldId = "hold_01HZX6N5V8B2H6M2K2D6YF6Y2V",
                ExpiresAt = DateTime.Parse("2026-07-26T18:05:00Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal),
                TableId = 12,
                SectionId = 4
            });
        }

        if (targetType == typeof(AvailabilityResponseDto))
        {
            return JsonSerializer.SerializeToNode(new AvailabilityResponseDto
            {
                RestaurantId = 7,
                Date = DateTime.Parse("2026-07-26T00:00:00Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal),
                Slots =
                [
                    new TimeSlotDto
                    {
                        Time = "18:00",
                        IsAvailable = true,
                        AvailableTableIds = [12, 14],
                        Category = "Dinner"
                    },
                    new TimeSlotDto
                    {
                        Time = "18:30",
                        IsAvailable = false,
                        AvailableTableIds = [],
                        Category = "Dinner"
                    }
                ]
            });
        }

        if (targetType == typeof(LookupDto))
        {
            return JsonSerializer.SerializeToNode(new LookupDto
            {
                Id = 7,
                Name = "Open Resto Downtown",
                ActiveBookingsCount = 12,
                BookingsPausedUntil = DateTime.Parse("2026-07-26T22:00:00Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal),
                IsArchived = false
            });
        }

        if (targetType == typeof(AdminOverviewDto))
        {
            return JsonSerializer.SerializeToNode(new AdminOverviewDto
            {
                TotalRestaurants = 3,
                TotalBookings = 142,
                TodayBookings = 18,
                TotalSeats = 96,
                ActiveHoldsCount = 2,
                PausedRestaurantsCount = 0,
                OccupancyData = [52, 64, 71],
                OccupancyDates = ["2026-07-24", "2026-07-25", "2026-07-26"],
                OccupancyCounts = [9, 11, 14],
                TodayBookingsList = [CreateBookingDetail()]
            });
        }

        if (targetType == typeof(AdminUserDto))
        {
            return JsonSerializer.SerializeToNode(new AdminUserDto
            {
                Id = 4,
                Email = "viewer@example.com",
                Role = AdminRole.BookingViewer,
                IsActive = true
            });
        }

        if (targetType == typeof(PvqStatusDto))
        {
            return JsonSerializer.SerializeToNode(new PvqStatusDto
            {
                IsConfigured = true,
                Question = "What city did you open your first restaurant in?"
            });
        }

        if (targetType == typeof(SocialLinkDto))
        {
            return JsonSerializer.SerializeToNode(new SocialLinkDto
            {
                Id = 8,
                Label = "Instagram",
                Url = "https://instagram.com/openresto",
                IconKey = "logo-instagram",
                SortOrder = 1
            });
        }

        if (targetType == typeof(HighlightDto))
        {
            return JsonSerializer.SerializeToNode(new HighlightDto
            {
                Id = 5,
                Title = "Fast confirmations",
                Body = "Guests get an instant confirmation without calling the host stand.",
                IconKey = "flash-outline",
                SortOrder = 1,
                Link = "https://openresto.example/features"
            });
        }

        if (targetType == typeof(EmailSettingsResponse))
        {
            return JsonSerializer.SerializeToNode(new EmailSettingsResponse
            {
                Host = "smtp.example.com",
                Port = 587,
                Username = "notifications@example.com",
                Password = "••••••••",
                EnableSsl = true,
                FromName = "Open Resto",
                FromEmail = "bookings@example.com",
                IsConfigured = true,
                SendBookingConfirmations = true
            });
        }

        if (targetType == typeof(EmailFailureResponse))
        {
            return JsonSerializer.SerializeToNode(new EmailFailureResponse
            {
                Id = 11,
                BookingRef = "BR-7H4K9Q",
                RecipientEmail = "guest@example.com",
                ErrorMessage = "SMTP timeout while delivering the booking confirmation.",
                AttemptedAt = DateTime.Parse("2026-07-26T17:05:00Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal)
            });
        }

        if (targetType == typeof(AdminNotificationDto))
        {
            return JsonSerializer.SerializeToNode(CreateNotification());
        }

        if (targetType == typeof(CachedBookingEntry))
        {
            return JsonSerializer.SerializeToNode(new CachedBookingEntry(
                BookingRef: "BR-7H4K9Q",
                Email: "guest@example.com",
                Date: "2026-07-26T18:00:00.0000000Z",
                Seats: 4,
                RestaurantName: "Open Resto Downtown",
                CreatedAt: "2026-07-26T17:30:00.0000000Z"));
        }

        if (targetType.IsArray)
        {
            return CreateArrayExample(targetType.GetElementType()!);
        }

        if (targetType != typeof(string)
            && typeof(IEnumerable).IsAssignableFrom(targetType)
            && targetType.IsGenericType)
        {
            return CreateArrayExample(targetType.GetGenericArguments()[0]);
        }

        return null;
    }

    public static JsonNode? CreateForStatus(int statusCode, IReadOnlyList<Type> responseTypes)
    {
        if (responseTypes.Count > 0)
        {
            Type preferred = responseTypes.Contains(typeof(ValidationProblemDetails))
                ? typeof(ValidationProblemDetails)
                : responseTypes[0];

            if (preferred == typeof(ProblemDetails))
            {
                return statusCode switch
                {
                    StatusCodes.Status401Unauthorized => JsonSerializer.SerializeToNode(new
                    {
                        type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                        title = "Unauthorized",
                        status = 401,
                        detail = "Authentication is required to access this resource.",
                        instance = "/api/example"
                    }),
                    StatusCodes.Status403Forbidden => JsonSerializer.SerializeToNode(new
                    {
                        type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                        title = "Forbidden",
                        status = 403,
                        detail = "The authenticated user is not allowed to access this resource.",
                        instance = "/api/example"
                    }),
                    StatusCodes.Status404NotFound => JsonSerializer.SerializeToNode(new
                    {
                        type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                        title = "Not Found",
                        status = 404,
                        detail = "The requested resource was not found.",
                        instance = "/api/example"
                    }),
                    StatusCodes.Status429TooManyRequests => JsonSerializer.SerializeToNode(new
                    {
                        type = "https://tools.ietf.org/html/rfc6585#section-4",
                        title = "Too Many Requests",
                        status = 429,
                        detail = "Rate limit exceeded. Retry later.",
                        instance = "/api/example"
                    }),
                    _ => CreateForType(preferred)
                };
            }

            JsonNode? typedExample = CreateForType(preferred);
            if (typedExample != null)
            {
                return typedExample;
            }
        }

        return statusCode switch
        {
            StatusCodes.Status204NoContent => null,
            StatusCodes.Status401Unauthorized => JsonSerializer.SerializeToNode(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                title = "Unauthorized",
                status = 401,
                detail = "Authentication is required to access this resource.",
                instance = "/api/example"
            }),
            StatusCodes.Status403Forbidden => JsonSerializer.SerializeToNode(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                title = "Forbidden",
                status = 403,
                detail = "The authenticated user is not allowed to access this resource.",
                instance = "/api/example"
            }),
            StatusCodes.Status404NotFound => JsonSerializer.SerializeToNode(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                title = "Not Found",
                status = 404,
                detail = "The requested resource was not found.",
                instance = "/api/example"
            }),
            StatusCodes.Status429TooManyRequests => JsonSerializer.SerializeToNode(new
            {
                type = "https://tools.ietf.org/html/rfc6585#section-4",
                title = "Too Many Requests",
                status = 429,
                detail = "Rate limit exceeded. Retry later.",
                instance = "/api/example"
            }),
            StatusCodes.Status500InternalServerError => JsonSerializer.SerializeToNode(new MessageResponse
            {
                Message = "An unexpected error occurred."
            }),
            _ => null
        };
    }

    private static JsonNode CreateArrayExample(Type elementType)
    {
        JsonNode? item = CreateForType(elementType);
        return item == null ? new JsonArray() : new JsonArray(item);
    }

    private static RestaurantDto CreateRestaurant() => new()
    {
        Id = 7,
        Name = "Open Resto Downtown",
        Address = "123 Main Street",
        OpenTime = "11:00",
        CloseTime = "22:00",
        OpenHours =
        [
            new DayHoursDto { Day = 1, Open = "11:00", Close = "22:00" },
            new DayHoursDto { Day = 2, Open = "11:00", Close = "22:00" },
            new DayHoursDto { Day = 3, Open = "11:00", Close = "22:00" },
            new DayHoursDto { Day = 4, Open = "11:00", Close = "22:00" },
            new DayHoursDto { Day = 5, Open = "11:00", Close = "23:00" },
            new DayHoursDto { Day = 6, Open = "10:00", Close = "23:00" },
            new DayHoursDto { Day = 7, Open = "10:00", Close = "21:00" }
        ],
        OpenDays = "1,2,3,4,5,6,7",
        Timezone = "America/New_York",
        Tags = ["brunch", "patio", "cocktails"],
        ImageUrl = "/media/location-7.webp?v=1764518400000",
        Description = "Seasonal small plates and a bright patio.",
        MenuUrl = "/media/menu-7.pdf?v=1764518400000",
        IsArchived = false,
        WalkInOnly = false,
        WalkInDays = "",
        DefaultBookingDurationMinutes = 90,
        BookingSlotIntervalMinutes = 30,
        MaxTableOversizeSeats = 2,
        Sections = [CreateSection()]
    };

    private static SectionDto CreateSection() => new()
    {
        Id = 4,
        Name = "Patio",
        SortOrder = 1,
        Tables =
        [
            new TableDto
            {
                Id = 12,
                Name = "Patio 4",
                Seats = 4
            }
        ]
    };

    private static BookingDto CreateBooking() => new()
    {
        Id = 18,
        RestaurantId = 7,
        SectionId = 4,
        TableId = 12,
        Date = DateTime.Parse("2026-07-26T18:00:00Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal),
        CustomerEmail = "guest@example.com",
        CustomerName = "Jordan Lee",
        Seats = 4,
        isHeld = false,
        SpecialRequests = "Window seat if available.",
        BookingRef = "BR-7H4K9Q",
        EndTime = DateTime.Parse("2026-07-26T19:30:00Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal),
        TableName = "Patio 4",
        SectionName = "Patio",
        TableSeats = 4,
        IsCancelled = false,
        CancelledAt = null
    };

    private static BookingDetailDto CreateBookingDetail() => new()
    {
        Id = 18,
        RestaurantId = 7,
        RestaurantName = "Open Resto Downtown",
        SectionId = 4,
        SectionName = "Patio",
        TableId = 12,
        TableName = "Patio 4",
        Date = DateTime.Parse("2026-07-26T18:00:00Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal),
        EndTime = DateTime.Parse("2026-07-26T19:30:00Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal),
        CustomerEmail = "guest@example.com",
        CustomerName = "Jordan Lee",
        Seats = 4,
        SpecialRequests = "Birthday dessert, please.",
        BookingRef = "BR-7H4K9Q",
        IsCancelled = false,
        CancelledAt = null
    };

    private static AdminNotificationDto CreateNotification() => new(
        Id: 32,
        RestaurantId: 7,
        RestaurantName: "Open Resto Downtown",
        BookingId: 18,
        BookingRef: "BR-7H4K9Q",
        Type: "BookingCreated",
        CustomerName: "Jordan Lee",
        BookingDate: DateTime.Parse("2026-07-26T18:00:00Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal),
        Seats: 4,
        IsRead: false,
        CreatedAt: DateTime.Parse("2026-07-26T17:00:00Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal),
        PushSentAt: DateTime.Parse("2026-07-26T17:00:01Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal),
        PushError: null);
}
