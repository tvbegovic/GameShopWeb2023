using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace GameshopWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IConfiguration configuration;

        public OrderController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpPost("")]
        public Order Create(Order order)
        {
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                db.Open();
                var tr = db.BeginTransaction();
                try
                {
                    if(order.User.Id > 0)
                    {
                        order.IdUser = order.User.Id;
                        db.Execute(
                            @"UPDATE [User] SET firstname = @firstname, lastname = @lastname,
                                address = @address, city = @city WHERE id = @id", order.User,tr);
                    }
                    else
                    {
                        order.IdUser = db.ExecuteScalar<int>(
                        @"INSERT INTO [User] (firstname, lastname, address, email, City) OUTPUT inserted.id
                        VALUES(@firstname, @lastname, @address, @email, @City)", order.User, tr
                        );
                    }
                    
                    order.DateOrdered = DateTime.Now;
                    order.Id = db.ExecuteScalar<int>(
                        "INSERT INTO [Order](idUser, dateOrdered) OUTPUT inserted.id VALUES(@idUser, @dateOrdered)", order, tr
                        );
                    foreach (var od in order.Details)
                    {
                        od.IdOrder = order.Id;
                        db.Execute(
                         @"INSERT INTO OrderDetail(idGame, idOrder, quantity, unitprice) 
                        VALUES(@idGame, @idOrder, @quantity, @unitprice)", od, tr
                            );
                    }
                    tr.Commit();
                    return order;
                }
                catch (Exception)
                {
                    tr.Rollback();
                    throw;
                }
            
            }
        }

        [HttpGet("forUser/{idUser}")]
        public List<Order> GetOrdersForUser(int idUser)
        {
            using (var db = new SqlConnection(configuration.GetConnectionString("connString")))
            {
                var orders = db.Query<Order>(
                    "SELECT * FROM [Order] WHERE idUser = @idUser", new { idUser }).ToList();
                var orderIds = orders.Select(o => o.Id).ToList();
                var details = db.Query<OrderDetail>(
                    "SELECT * FROM OrderDetail WHERE idOrder IN @orderIds", new { orderIds }
                    );
                foreach(var order in orders)
                {
                    order.Details = details.Where(d => d.IdOrder == order.Id).ToList();
                }
                return orders;
            }
        }
    }
}
