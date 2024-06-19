using Microsoft.AspNetCore.Http.HttpResults;
using System.Net.Mail;
// var newGUID = Guid.NewGuid();
var builder = WebApplication.CreateBuilder(args);
// var  MyAllowSpecificOrigins = "_myAllowedOrigins";
builder.Services.AddSingleton<NoteInterface>(new NoteAbstraction());
builder.Services.AddCors(options => {
    options.AddPolicy("AllowedOrigins", policy => {
        policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod();
    });
});
// builder.Services.AddCors();
var app = builder.Build();
// app.UseHttpsRedirection();
// app.UseRouting();
app.UseCors("AllowedOrigins");
// app.UseAuthorization();
var notes = new List<Note>();
var subscribers = new List<Subscriber>();
app.MapGet("/notes", (NoteInterface abstraction) => abstraction.GetAllNotes());
// app.MapGet("/notes/{id}", )
app.MapPost("/add", Results<Created<Note>, BadRequest<string>>(Note note, NoteInterface abstraction) => {
    note = note with {id = Guid.NewGuid()};
    var result = abstraction.AddNote(note);
    return (result != null) ? TypedResults.Created("/{id}", note): TypedResults.BadRequest("Note Id is not unique. Already exists!");
})
// End Point Filter
.AddEndpointFilter(async (context, next) => {
    var note = context.GetArgument<Note>(0);
    var errors = new Dictionary<string, string[]>();
    if (note.dueDate.Date < DateTime.UtcNow.Date)
    {
        errors.Add(nameof(Note.dueDate), [$"Cannot have due date in the past. Current time: {DateTime.UtcNow.Date}. Date provided: {note.dueDate.Date}"]);
    }
    if (note.isCompleted)
    {
        errors.Add(nameof(Note.isCompleted), ["Cannot add completed todo"]);
    }
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }
    return await next(context);
});
app.MapPost("/subscribers", (Subscriber subscriber) => {
    var found = subscribers.SingleOrDefault(s => s.email == subscriber.email && s.noteId == subscriber.noteId);
    // if (found == null)
    // {
        subscribers.Add(subscriber);
        return TypedResults.Created("/{email}", subscriber);
    // }
    // else
    // {
        // return TypedResults.BadRequest("Error");
    // }
});
// app.MapPost("/addSubscriber", Results<Created<Person>, BadRequest<string>>(Person person, SubscriberInterface abstraction) => {
//     person = person with {pid = Guid.NewGuid()};
//     var result = abstraction.AddSubscriber(person);
//     return (result != null) ? TypedResults.Created("/{id}", person): TypedResults.BadRequest("Error");

// });
// app.MapDelete("/removeSubscriber/{pid}", (Guid id, SubscriberInterface abstraction)=> {
//     abstraction.DeleteSubscriber(id);
//     return TypedResults.NoContent();
// });
app.MapGet("/notes/{id}", Results<Ok<Note>, NotFound> (Guid id, NoteInterface abstraction) => {
    var fetchedNote = abstraction.GetNoteById(id);
    return (fetchedNote is null) ? TypedResults.NotFound() : TypedResults.Ok(fetchedNote);
});

app.MapDelete("/notes/{id}", (Guid id, NoteInterface abstraction)=>{
    abstraction.DeleteNote(id);
    return TypedResults.NoContent();
});
app.Run();
// tags, priority, dependentOn (Note), reminderLocation, urgent?, createdAt, editedAt, EditNote
public record Note(Guid id, string name, string description, DateTime dueDate, bool isCompleted);
public record Person(Guid pid, MailAddress address, List<Note> subscribedNotes);
public record Subscriber(string email, string noteId);
interface NoteInterface {
    Note? GetNoteById(Guid id);
    List<Note> GetAllNotes();
    Note? AddNote(Note note);
    void DeleteNote(Guid id);

}
interface SubscriberInterface {
    void DeleteSubscriber(Guid id);
    Person AddSubscriber(Person person);
}
class NoteAbstraction : NoteInterface {
    // TODO: Implement database connection and change each method
    private readonly List<Note> _notes = [];
    public Note? GetNoteById(Guid id) {
        // TODO: Call from databse
        return _notes.SingleOrDefault(note => note.id == id);
    }
    public List<Note> GetAllNotes() {
        // TODO: Call from databse
        return _notes;
    }
    public Note? AddNote(Note note) {
        // TODO: Call from databse
        bool exists = GetNoteById(note.id) != null;
        if (exists) {
            return null;
        }
        _notes.Add(note);
        return note;
    }
    public void DeleteNote(Guid id) {
        // TODO: Call from databse
        _notes.RemoveAll(note => note.id == id);
        return;
    }
}
