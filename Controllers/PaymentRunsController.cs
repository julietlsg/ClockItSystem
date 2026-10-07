using ClockItSystem.Data;
using ClockItSystem.Interfaces;
using ClockItSystem.Models;
using ClockItSystem.Models.Enums;
using ClockItSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClockItSystem.Controllers
{
    [Authorize(Roles = "Admin,Project Manager")]
    public class PaymentRunsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IClientAccessService _clientAccessService;

        public PaymentRunsController(
            ApplicationDbContext context,
            IClientAccessService clientAccessService)
        {
            _context = context;
            _clientAccessService = clientAccessService;
        }

        // ============================================================
        // PAYMENT RUN LIST
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var accessibleClientIds =
                await _clientAccessService
                    .GetAccessibleClientIdsAsync();

            var paymentRuns = await _context.StipendPaymentRuns
                .AsNoTracking()
                .Include(x => x.Client)
                .Where(x =>
                    accessibleClientIds.Contains(x.ClientId))
                .OrderByDescending(x => x.PeriodFrom)
                .ThenBy(x => x.Client.Name)
                .ToListAsync();

            return View(paymentRuns);
        }

        // ============================================================
        // CREATE PAYMENT RUN - FILTER PAGE
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new PaymentRunViewModel
            {
                PaymentPeriod = new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1)
            };

            await PopulateFilters(model);

            return View(model);
        }

        // ============================================================
        // CREATE PAYMENT RUN
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PaymentRunViewModel model)
        {
            await PopulateFilters(model);

            if (!ModelState.IsValid)
                return View(model);

            if (!model.ClientId.HasValue ||
                !model.PaymentPeriod.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please select a client and payment period.");

                return View(model);
            }

            // --------------------------------------------------------
            // SECURITY CHECK
            // --------------------------------------------------------

            if (!await _clientAccessService
                .CanAccessClientAsync(model.ClientId.Value))
            {
                return Forbid();
            }

            // --------------------------------------------------------
            // DETERMINE PAYMENT PERIOD
            // --------------------------------------------------------

            var periodFrom = new DateTime(
                model.PaymentPeriod.Value.Year,
                model.PaymentPeriod.Value.Month,
                1);

            var periodTo = periodFrom
                .AddMonths(1)
                .AddDays(-1);

            // Used when querying DateTime values so the entire
            // final day of the month is included.
            var periodEndExclusive = periodTo.AddDays(1);

            // --------------------------------------------------------
            // PREVENT DUPLICATE PAYMENT RUN
            // --------------------------------------------------------

            var existingRun = await _context.StipendPaymentRuns
                .AnyAsync(x =>
                    x.ClientId == model.ClientId.Value &&
                    x.PeriodFrom == periodFrom &&
                    x.PeriodTo == periodTo);

            if (existingRun)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "A payment run already exists for the selected client and payment period.");

                return View(model);
            }

            // --------------------------------------------------------
            // GET CLIENT
            // --------------------------------------------------------

            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ClientId == model.ClientId.Value &&
                    x.IsActive);

            if (client == null)
            {
                ModelState.AddModelError(
                    nameof(model.ClientId),
                    "Selected client could not be found.");

                return View(model);
            }

            // --------------------------------------------------------
            // GET STIPEND RATE
            // --------------------------------------------------------

            var dailyRate = await GetDailyRateAsync(
                model.ClientId.Value,
                periodFrom,
                periodTo);

            if (!dailyRate.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No active stipend rate was found for the selected client and payment period.");

                return View(model);
            }

            // --------------------------------------------------------
            // GET ACTIVE STUDENTS
            // --------------------------------------------------------

            var students = await _context.Students
                .AsNoTracking()
                .Where(s =>
                    s.ClientId == model.ClientId.Value &&
                    s.IsActive)
                .Include(s => s.Bank)
                .Include(s => s.BankBranch)
                .Include(s => s.AccountType)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            if (!students.Any())
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No active students were found for the selected client.");

                return View(model);
            }

            // --------------------------------------------------------
            // CREATE PAYMENT RUN
            // --------------------------------------------------------

            var paymentRun = new StipendPaymentRun
            {
                ClientId = client.ClientId,

                PeriodFrom = periodFrom,

                PeriodTo = periodTo,

                Status = "Draft",

                CreatedBy =
                    User.Identity?.Name ?? "System",

                CreatedAt = DateTime.Now
            };

            _context.StipendPaymentRuns.Add(paymentRun);

            await _context.SaveChangesAsync();

            // --------------------------------------------------------
            // PROCESS EACH STUDENT
            // --------------------------------------------------------

            var paymentRows = new List<StipendPayment>();

            foreach (var student in students)
            {
                var payment =
                    await BuildStudentPaymentAsync(
                        student,
                        paymentRun.Id,
                        dailyRate.Value,
                        periodFrom,
                        periodEndExclusive);

                if (payment != null)
                {
                    paymentRows.Add(payment);
                }
            }

            // --------------------------------------------------------
            // SAVE PAYMENT ROWS
            // --------------------------------------------------------

            if (paymentRows.Any())
            {
                await _context.StipendPayments
                    .AddRangeAsync(paymentRows);

                paymentRun.TotalStudents =
                    paymentRows.Count;

                paymentRun.TotalEligibleDays =
                    paymentRows.Sum(
                        x => x.TotalEligibleDays);

                paymentRun.TotalAmount =
                    paymentRows.Sum(
                        x => x.StipendAmount);
            }
            else
            {
                paymentRun.TotalStudents = 0;
                paymentRun.TotalEligibleDays = 0;
                paymentRun.TotalAmount = 0;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = paymentRun.Id
                });
        }

        // ============================================================
        // PAYMENT RUN DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var paymentRun =
                await _context.StipendPaymentRuns
                    .AsNoTracking()
                    .Include(x => x.Client)
                    .Include(x => x.Payments)
                        .ThenInclude(x => x.Student)
                    .FirstOrDefaultAsync(x =>
                        x.Id == id);

            if (paymentRun == null)
                return NotFound();

            // --------------------------------------------------------
            // SECURITY CHECK
            // --------------------------------------------------------

            if (!await _clientAccessService
                .CanAccessClientAsync(
                    paymentRun.ClientId))
            {
                return Forbid();
            }

            // --------------------------------------------------------
            // BUILD VIEW MODEL
            // --------------------------------------------------------

            var model =
                new PaymentRunResultViewModel
                {
                    PaymentRunId =
                        paymentRun.Id,

                    ClientId =
                        paymentRun.ClientId,

                    ClientName =
                        paymentRun.Client?.Name
                        ?? string.Empty,

                    PeriodFrom =
                        paymentRun.PeriodFrom,

                    PeriodTo =
                        paymentRun.PeriodTo,

                    TotalStudents =
                        paymentRun.TotalStudents,

                    TotalEligibleDays =
                        paymentRun.TotalEligibleDays,

                    TotalAmount =
                        paymentRun.TotalAmount,

                    Status =
                        paymentRun.Status,

                    Students =
                        paymentRun.Payments
                            .OrderBy(x =>
                                x.Student.LastName)
                            .ThenBy(x =>
                                x.Student.FirstName)
                            .Select(x =>
                                new PaymentRunStudentViewModel
                                {
                                    StudentId =
                                        x.StudentId,

                                    StudentNumber =
                                        x.Student.StudentNumber,

                                    StudentName =
                                        $"{x.Student.FirstName} {x.Student.LastName}",

                                    EligibleAttendanceDays =
                                        x.EligibleAttendanceDays,

                                    LeaveDays =
                                        x.LeaveDays,

                                    SickLeaveDays =
                                        x.SickLeaveDays,

                                    FamilyResponsibilityLeaveDays =
                                        x.FamilyResponsibilityLeaveDays,

                                    TotalEligibleDays =
                                        x.TotalEligibleDays,

                                    DailyRate =
                                        x.DailyRate,

                                    StipendAmount =
                                        x.StipendAmount,

                                    BankName =
                                        x.BankName,

                                    BranchCode =
                                        x.BranchCode,

                                    AccountNumber =
                                        x.AccountNumber,

                                    AccountType =
                                        x.AccountType,

                                    AccountHolderName =
                                        x.AccountHolderName,

                                    Status =
                                        x.Status
                                })
                            .ToList()
                };

            return View(model);
        }

        // ============================================================
        // POPULATE FILTERS
        // ============================================================

        private async Task PopulateFilters(
            PaymentRunViewModel model)
        {
            var accessibleClientIds =
                await _clientAccessService
                    .GetAccessibleClientIdsAsync();

            model.Clients =
                await _context.Clients
                    .Where(x =>
                        x.IsActive &&
                        accessibleClientIds.Contains(
                            x.ClientId))
                    .OrderBy(x => x.Name)
                    .Select(x =>
                        new SelectListItem
                        {
                            Value =
                                x.ClientId.ToString(),

                            Text =
                                x.Name
                        })
                    .ToListAsync();
        }

        // ============================================================
        // GET DAILY STIPEND RATE
        // ============================================================

        private async Task<decimal?> GetDailyRateAsync(
            int clientId,
            DateTime periodFrom,
            DateTime periodTo)
        {
            var rate =
                await _context.ClientStipendRates
                    .AsNoTracking()
                    .Where(x =>
                        x.ClientId == clientId &&
                        x.IsActive &&
                        x.EffectiveFrom <= periodTo &&
                        (!x.EffectiveTo.HasValue ||
                         x.EffectiveTo.Value >= periodFrom))
                    .OrderByDescending(
                        x => x.EffectiveFrom)
                    .FirstOrDefaultAsync();

            return rate?.DailyRate;
        }

        // BUILD STUDENT PAYMENT
        private async Task<StipendPayment?> BuildStudentPaymentAsync(
            Student student,
            int paymentRunId,
            decimal dailyRate,
            DateTime periodFrom,
            DateTime periodEndExclusive)
        {
            var attendanceRecords =
                await _context.AttendanceRecords
                    .AsNoTracking()
                    .Include(x =>
                        x.AttendanceApproval)
                    .Where(x =>
                        x.StudentId == student.Id &&
                        x.ClientId == student.ClientId &&
                        x.AttendanceDate >= periodFrom &&
                        x.AttendanceDate < periodEndExclusive)
                    .ToListAsync();

            // APPROVED ATTENDANCE

            var approvedAttendanceDays =
                attendanceRecords.Count(x =>
                    x.Status == "Approved" &&
                    x.AttendanceApproval != null &&
                    x.AttendanceApproval.IsApproved);

            // APPROVED LEAVE

            var leaveDays =
                attendanceRecords.Count(x =>
                    x.Status == "Rejected" &&
                    x.AttendanceApproval != null &&
                    !x.AttendanceApproval.IsApproved &&
                    x.AttendanceApproval.Reason ==
                        DataEnums.AttendanceRejectionReason.Leave);

            // APPROVED SICK LEAVE

            var sickLeaveDays =
                attendanceRecords.Count(x =>
                    x.Status == "Rejected" &&
                    x.AttendanceApproval != null &&
                    !x.AttendanceApproval.IsApproved &&
                    x.AttendanceApproval.Reason ==
                        DataEnums.AttendanceRejectionReason.SickLeave);

            // APPROVED FAMILY RESPONSIBILITY LEAVE

            var familyResponsibilityLeaveDays =
                attendanceRecords.Count(x =>
                    x.Status == "Rejected" &&
                    x.AttendanceApproval != null &&
                    !x.AttendanceApproval.IsApproved &&
                    x.AttendanceApproval.Reason ==
                        DataEnums.AttendanceRejectionReason
                            .FamilyResponsibilityLeave);

            // STUDENT ABSENT

            // StudentAbsent is intentionally NOT included
            // in the eligible payment days.

            // TOTAL ELIGIBLE DAYS

            var totalEligibleDays =
                approvedAttendanceDays +
                leaveDays +
                sickLeaveDays +
                familyResponsibilityLeaveDays;

            // STIPEND CALCULATION

            var stipendAmount =
                totalEligibleDays * dailyRate;

            // NO PAYABLE DAYS

            if (totalEligibleDays == 0)
            {
                return null;
            }

            // CREATE PAYMENT SNAPSHOT

            return new StipendPayment
            {
                PaymentRunId =
                    paymentRunId,

                StudentId =
                    student.Id,

                EligibleAttendanceDays =
                    approvedAttendanceDays,

                LeaveDays =
                    leaveDays,

                SickLeaveDays =
                    sickLeaveDays,

                FamilyResponsibilityLeaveDays =
                    familyResponsibilityLeaveDays,

                TotalEligibleDays =
                    totalEligibleDays,

                DailyRate =
                    dailyRate,

                StipendAmount =
                    stipendAmount,

                // BANKING SNAPSHOT

                BankName =
                    student.Bank?.BankName,

                BranchCode =
                    student.BankBranch?.BranchCode,

                AccountNumber =
                    student.AccountNumber,

                AccountType =
                    student.AccountType?.AccountTypeName,

                AccountHolderName =
                    student.AccountHolderName,

                // PAYMENT STATUS
                Status = "Pending"
            };
        }
    }
}