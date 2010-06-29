all: WebServiceAPI.pdf

WebServiceAPI.pdf: dirty
	doxygen WebService.doxygen
	(cd ../obj/latex; make pdf && mv refman.pdf ../../documentation/WebService.pdf)

dirty:

clean:
	rm -rf WebServiceAPI.pdf
