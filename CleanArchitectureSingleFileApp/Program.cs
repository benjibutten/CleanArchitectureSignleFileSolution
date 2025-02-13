using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

//N�dv�ndiga beroenden f�r API delen, Controllers, OpenApi/Scalar
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Registrera beroenden (Application & Infrastructure) 
// Kan ske i extention methods som ligger i respektive projekt

//Registration Infra beroenden 
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();

//Registrera Application beroenden
builder.Services.AddTransient<IProductService, ProductService>();



var app = builder.Build();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    _ = endpoints.MapControllers();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service; //<--- Presentationslagret pratar bara med applikationslagret

    public ProductsController(IProductService productService)
    {
        _service = productService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Product>> Get() //Product ligger i Dom�nlagret s� det �r okej, men problemet h�r �r att det �r samma som databasen anv�nder, egentligen vill man ha DTOs eller "Contracts" s� man kan v�lja vad man vill exponera ut�t
    {
        return Ok(_service.GetAllProducts());
    }

    [HttpPost]
    public ActionResult<Product> Post(Product product)
    {
        _service.AddProduct(product);
        return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }
}

// --- Dom�nlagret --- 
// Dom�nlagret f�r inte bero p� n�got annat lager.
// Exempel p� saker man kan ha i dom�nlagret �r: Entiteter/modeller, v�rdeobjekt som anv�nds i entiterna/modellerna t.ex Enums eller annat som �r kopplat till dom�nlogiken, tj�nster f�r dom�nlogik, interfaces
public class Product
{
    private static int _nextId = 1;

    public Product()
    {
        Id = _nextId++;
    }

    public int Id { get; private set; }
    public string Name { get; set; }
}

// Repository interfacet
public interface IProductRepository
{
    IEnumerable<Product> GetAll();
    void Add(Product product);
}

// --- Applikationslagret ---
public interface IProductService // <-- Applikations "use cases" som anv�nds i presentations lagret i detta fall en API controller
{
    IEnumerable<Product> GetAllProducts();
    void AddProduct(Product product);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _repository; //<--- Applikationslagret f�r bero p� Dom�nlagret

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<Product> GetAllProducts()
    {
        return _repository.GetAll();
    }

    public void AddProduct(Product product)
    {
        _repository.Add(product);
    }
}


// --- Infrastruktur-lagret ---
public class InMemoryProductRepository : IProductRepository // <-- Infra f�r bero p� Dom�n, men inte applikations lagret
{
    //"Databasen"
    private readonly List<Product> _products = [];

    public InMemoryProductRepository()
    {
        //Seed data
        _products.Add(new Product { Name = "Product 1" });
        _products.Add(new Product { Name = "Product 2" });
        _products.Add(new Product { Name = "Product 3" });
    }

    public IEnumerable<Product> GetAll()
    {
        return _products;
    }

    public void Add(Product product)
    {
        _products.Add(product);
    }
}
