Form Storage V8 in an Umbraco version 8 compatible re-working of [Kevin Giszewski's](https://our.umbraco.org/member/51046) original [package](https://our.umbraco.org/projects/backoffice-extensions/form-storage/).

As per the original, the new package offers a simple way to take user submitted data and store it into your database,  perfect for use with MVC Surface Controller forms.

This package comes with a datatype that you can place on any document to search the data.

<u>Installation:</u>

On install, this package will add three tables to your database.  You will need to remove these tables manually on uninstall (this prevents unintended data destruction).

You will need to add a few AppSettings keys to your web config.

Use one key to setup each form with it's fields like so:

```
<add key="FormStorage:ContactUs" value="name,address,email,phone,company,comments"/>
```

This adds the '**ContactUs**' form with the fields name, address, etc.  You may house as many forms as you wish.

<u>Configuration:</u>

You can optionally provide some configuration of how your fields are displayed when form submissions are listed in your back-office. The first example shows how to provide some more human-friendly names for your fields:

    <add key="FormStorage:Translation:name" value="Name"/>
    <add key="FormStorage:Translation:email" value="Email Address"/>
    <add key="FormStorage:Translation:phone" value="Phone"/>
    <add key="FormStorage:Translation:address" value="Address"/>
    <add key="FormStorage:Translation:comments" value="Comments"/>
    <add key="FormStorage:Translation:company" value="Company"/>

I recommend you create very simple field names that resemble the data closely.  The more human friendly names can change without any consequences to the data. N.B. these field name translations are used across all of your forms.



The V8 package offers some further configuration options:

1. The width of a field (in pixels) can be set (default is *100*):

   ```
   <add key="FormStorage:YourFormAlias:YourFieldName:width" value="150"/>
   ```

   

2. The horizontal alignment of a field can be set to *left*, *right* or *center* (default is *center*):

   ```
   <add key="FormStorage:YourFormAlias:YourFieldName:align" value="left"/>
   ```

   

3. The visibility of a field can be set to *true* or *false* (default is *true*):

   ```
   <add key="FormStorage:YourFormAlias:YourFieldName:visible" value="false"/>
   ```



<u>Wire up your form:</u>

In your MVC Surface Controller, add something like this to save submissions:

```
using FormStorage;

FormSchema.CreateSubmission("ContactUs",
	new Dictionary<string, string> {
        { "name", m.Name },
        { "email", m.Email },
        { "address", m.Address},
        { "company", m.Company},
        { "phone", m.Phone},
        { "comments", m.Comments}
 });
```



<u>Search the Results:</u>

Create a datatype instance using "Form Storage", set the prevalue to your form alias and then drop the datatype onto a doctype of your choosing.

A sample datatype has been included with the install.

Many thanks again to Kevin for his excellent, very useful original!
