(import 
  (ironscheme)
  (ironscheme web))

  
(define (method-post?)
  (eqv? (method) "POST"))  
  
(define (method-get?)
  (eqv? (method) "GET"))  
  
(define xhtml "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\n")

(define title "hello world")

(display xhtml)

(display-html 
  `(html (xmlns . "http://www.w3.org/1999/xhtml")
    (head (title ,title))
    (body
      (form (id . "form1") (method . "post")
        (h1 ,title)
        (p "rabble rabble")
				,(if (and (method-post?) (form "foo"))
						'(input (type . "submit") (name . "bar") (value . "Now you can"))
        		'(input (type . "submit") (name . "bar") (value . "Can't click me") (disabled . #t)))
        (br)
        (input (type . "submit") (name . "foo") (value . "Click me!") )
        (br)
        (p "bar = " ,(form "bar"))
        (p "<strong>hello</strong>")
        (p "baz = " ,(form "baz"))
        (select (name . "baz") 
          ,@(map (lambda (x) 
                   `(option ,x (selected . ,(eqv? (form "baz") x))))
              '("good" "bad" "ugly") ) 
          (onchange . "submit()"))
        (p (a (href . ,(string-format "test.ss?id={0}" (+ 1 (string->number (querystring "id"))))) "Go back"))
        (p (a (href . ,(string-format "test.ss?id={0}" (+ 100 (string->number (querystring "id"))))) "Go forward"))
    ))))