import { Component, OnInit } from '@angular/core';
import { AccountService } from '../services/account.service';
import { Observable, of } from 'rxjs';
import { User } from '../_models/user';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { MembersService } from '../services/members.service';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css'],
})
export class NavComponent implements OnInit {
  model: any = {};
  currentUser$: Observable<User | null> = of(null);
  constructor(
    private accountService: AccountService,
    private memberService: MembersService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.currentUser$ = this.accountService.currentUser$;
  }

  login(): void {
    this.accountService.login(this.model).subscribe({
      next: () => {
        this.memberService.resetFilter();
        this.router.navigate(['/', 'members']);
      },
      error: (error) => this.toastr.error(error.error),
    });
  }
  logout(): void {
    this.accountService.logout();
    this.memberService.resetFilter();
    this.router.navigateByUrl('/');
  }
}
