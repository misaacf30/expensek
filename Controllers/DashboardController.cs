using Expensek.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Expensek.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index()
        {
            List<Transaction> transactions = await _context.Transactions
                .Include(t => t.Category)
                .ToListAsync();

            int totalIncome = transactions
                .Where(t => t.Category?.Type == "Income")
                .Sum(t => t.Amount);

            int totalExpense = transactions
                .Where(t => t.Category?.Type == "Expense")
                .Sum(t => t.Amount);

            int balance = totalIncome - totalExpense;
            
            ViewBag.TotalIncome = totalIncome;
            ViewBag.TotalExpense = totalExpense;
            ViewBag.Balance = balance;

            // Chart Data
            DateTime startDate = DateTime.Today.AddDays(-6);
            DateTime endDate = DateTime.Today;

            List<SplineChartData> incomeSummary = transactions
                .Where(t => t.Category?.Type == "Income")
                .GroupBy(t => t.Date)
                .Select(t => new SplineChartData()
                {
                    day = t.First().Date.ToString("dd-MMM"),
                    income = t.Sum(x => x.Amount)
                })
                .ToList();

            List<SplineChartData> expenseSummary = transactions
                .Where(t => t.Category?.Type == "Expense")
                .GroupBy(t => t.Date)
                .Select(t => new SplineChartData()
                {
                    day = t.First().Date.ToString("dd-MMM"),
                    expense = t.Sum(x => x.Amount)
                })
                .ToList();

            string[] last7days = Enumerable.Range(0, 7)
                .Select(x => startDate.AddDays(x).ToString("dd-MMM"))
                .ToArray();

            ViewBag.ChartData = from day in last7days
                                      join income in incomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in expenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      { 
                                        day = day,
                                        income = income == null ? 0 : income.income,
                                        expense = expense == null ? 0 : expense.expense,
                                      };


            // Doughnut Data
            ViewBag.DoughnutData = transactions
                .Where(i => i.Category?.Type == "Expense")
                .GroupBy(j => j.Category?.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category?.Icon + " " + k.First().Category?.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C0"),
                })
                .OrderByDescending(l => l.amount)
                .ToList();

            // Recent Transactions
            ViewBag.RecentTransactions = await _context.Transactions
                .Include(i => i.Category)
                .OrderByDescending(j => j.Date)
                .Take(10)
                .ToListAsync();

            return View();
        }

        public class SplineChartData
        {
            public string day;
            public int income;
            public int expense;
        }
    }
}